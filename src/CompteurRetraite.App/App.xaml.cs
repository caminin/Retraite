using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CompteurRetraite.App.Bureau;
using CompteurRetraite.App.Systeme;
using CompteurRetraite.Core;
using CompteurRetraite.Core.MiseAJour;
using CompteurRetraite.Core.Phrases;

namespace CompteurRetraite.App;

public partial class App : System.Windows.Application
{
    public static new App Current => (App)System.Windows.Application.Current;

    public Donnees Donnees { get; private set; } = null!;
    public StockageDonnees Stockage { get; private set; } = null!;

    private SelecteurPhrase _selecteur = null!;
    private InstanceUnique? _instance;
    private IconeNotification? _icone;
    private Rafraichisseur? _rafraichisseur;
    private FenetreCompteur? _fenetre;
    private HwndSource? _ecouteurBarre;
    private bool _arretEnCours;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, ex) =>
        {
            Journal.Ecrire($"Erreur inattendue : {ex.Exception}");
            ex.Handled = true;
        };

        _instance = new InstanceUnique();
        if (!_instance.EstPremiere)
        {
            InstanceUnique.Envoyer("REGLAGES");
            Quitter();
            return;
        }
        _instance.CommandeRecue += c => Dispatcher.BeginInvoke(() => Executer(c));

        Stockage = new StockageDonnees(StockageDonnees.DossierParDefaut());
        Donnees = Stockage.Charger(DateOnly.FromDateTime(DateTime.Now));
        if (Environment.ProcessPath is { } exe) RemplacementExe.Nettoyer(exe);
        _selecteur = new SelecteurPhrase(BanquePhrases.Charger());

        CreerFenetre();
        _icone = new IconeNotification();
        _icone.ReglagesDemandes += () => OuvrirReglages(bienvenue: false);
        _icone.MasquageDemande += () =>
        {
            Donnees.CompteurMasque = !Donnees.CompteurMasque;
            Rafraichir();
        };
        _icone.QuitterDemande += Quitter;

        _rafraichisseur = new Rafraichisseur(Dispatcher, Rafraichir, () => _fenetre?.Rattacher());
        EcouterRedemarrageExplorateur();
        Rafraichir();

        if (e.Args.Contains("--bienvenue") || Donnees.Retraite is null) OuvrirReglages(bienvenue: true);
    }

    /// <summary>Recalcule et réaffiche le compteur. Appelée après chaque changement de réglage.</summary>
    public void Rafraichir()
    {
        if (_fenetre is null || _icone is null) return;
        var etat = Affichage.Calculer(Donnees, _selecteur, DateOnly.FromDateTime(DateTime.Now));
        Enregistrer();

        if (Donnees.CompteurMasque) _fenetre.Hide();
        else
        {
            if (!_fenetre.IsVisible) _fenetre.Show();
            _fenetre.Afficher(etat, Donnees.Apparence);
        }

        string infobulle = etat.Mode == Mode.Compte ? $"{etat.Chiffre} {etat.Libelle}" : etat.Phrase;
        _icone.MettreAJour(infobulle, Donnees.CompteurMasque);
    }

    public void Enregistrer()
    {
        try { Stockage.Enregistrer(Donnees); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Journal.Ecrire($"Enregistrement impossible : {e.Message}");
        }
    }

    public void OuvrirReglages(bool bienvenue)
    {
        // Tâche 11.
    }

    public void Quitter()
    {
        _arretEnCours = true;
        _rafraichisseur?.Dispose();
        _icone?.Dispose();
        _ecouteurBarre?.Dispose();
        _fenetre?.Close();
        _instance?.Dispose();
        Shutdown();
    }

    private void Executer(string commande)
    {
        switch (commande)
        {
            case "REGLAGES": OuvrirReglages(bienvenue: false); break;
            case "QUITTER": Quitter(); break;
        }
    }

    private void CreerFenetre()
    {
        _fenetre = new FenetreCompteur();
        // Si l'Explorateur redémarre, la fenêtre parente disparaît et emporte la nôtre : on la recrée.
        _fenetre.Closed += (_, _) =>
        {
            if (_arretEnCours) return;
            _fenetre = null;
            var minuterie = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            minuterie.Tick += (_, _) =>
            {
                minuterie.Stop();
                if (_arretEnCours || _fenetre is not null) return;
                CreerFenetre();
                Rafraichir();
            };
            minuterie.Start();
        };
        _fenetre.Show();
    }

    /// <summary>Fenêtre invisible de premier niveau qui reçoit le message « TaskbarCreated » envoyé au redémarrage de l'Explorateur.</summary>
    private void EcouterRedemarrageExplorateur()
    {
        uint taskbarCreated = Win32.RegisterWindowMessage("TaskbarCreated");
        _ecouteurBarre = new HwndSource(new HwndSourceParameters("CompteurRetraite.Ecouteur") { Width = 0, Height = 0, WindowStyle = 0 });
        _ecouteurBarre.AddHook((IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool traite) =>
        {
            if ((uint)msg == taskbarCreated)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (_fenetre is null) return;
                    _fenetre.Rattacher();
                    Rafraichir();
                });
            return IntPtr.Zero;
        });
    }
}
