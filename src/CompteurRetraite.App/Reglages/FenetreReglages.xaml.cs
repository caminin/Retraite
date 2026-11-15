using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CompteurRetraite.Core;

namespace CompteurRetraite.App.Reglages;

public partial class FenetreReglages : Window
{
    private static readonly CultureInfo Fr = new("fr-FR");
    private static readonly DayOfWeek[] JoursOuvres =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private bool _chargement;
    private static Donnees Donnees => App.Current.Donnees;
    private static DateOnly Aujourdhui => DateOnly.FromDateTime(DateTime.Now);

    public FenetreReglages(bool bienvenue)
    {
        _chargement = true;
        InitializeComponent();
        Language = XmlLanguage.GetLanguage("fr-FR");
        AjusterALaTailleDeLEcran();

        if (bienvenue)
        {
            Titre.Text = $"Bienvenue, {Donnees.Prenom} !";
            SousTitre.Text = "Indiquez votre premier jour de retraite : le compteur s'affichera sur votre bureau.";
        }

        ChampPrenom.Text = Donnees.Prenom;
        RemplirDate();

        foreach (var j in JoursOuvres)
            ChoixJourSemaine.Items.Add(new ComboBoxItem { Content = Majuscule(Fr.DateTimeFormat.GetDayName(j)), Tag = j });
        ChoixJourSemaine.SelectedIndex = 2; // mercredi

        // « En haut à gauche » n'est plus proposé : le compteur y recouvrirait les icônes du bureau.
        // Il reste affiché s'il avait déjà été choisi, pour ne rien perdre.
        var coins = new List<(Coin, string)> { (Coin.BasDroite, "En bas à droite"), (Coin.BasGauche, "En bas à gauche"), (Coin.HautDroite, "En haut à droite") };
        if (Donnees.Apparence.Coin == Coin.HautGauche) coins.Add((Coin.HautGauche, "En haut à gauche"));
        Remplir(ChoixCoin, Donnees.Apparence.Coin, coins.ToArray());
        Remplir(ChoixTaille, Donnees.Apparence.Taille, (Taille.Petit, "Petit"), (Taille.Moyen, "Moyen"), (Taille.Grand, "Grand"));
        Remplir(ChoixTheme, Donnees.Apparence.Theme, (Theme.Clair, "Blanc (fond d'écran foncé)"), (Theme.Fonce, "Noir (fond d'écran clair)"));
        ChoixCoin.SelectionChanged += (_, _) => Apparence(a => a.Coin = (Coin)Valeur(ChoixCoin));
        ChoixTaille.SelectionChanged += (_, _) => Apparence(a => a.Taille = (Taille)Valeur(ChoixTaille));
        ChoixTheme.SelectionChanged += (_, _) => Apparence(a => a.Theme = (Theme)Valeur(ChoixTheme));

        Calendrier.Change += Modifie;
        _chargement = false;
        MettreAJour();
    }

    /// <summary>Sur un petit écran (portable 1366 × 768, par exemple), la fenêtre ne dépasse jamais de la zone utile.</summary>
    private void AjusterALaTailleDeLEcran()
    {
        var zone = SystemParameters.WorkArea;
        Height = Math.Min(Height, zone.Height - 16);
        Width = Math.Min(Width, zone.Width - 16);
        MinHeight = Math.Min(MinHeight, Height);
    }

    // ---------- Date de retraite : trois listes ----------

    private void RemplirDate()
    {
        for (int j = 1; j <= 31; j++) ChoixJour.Items.Add(j);
        for (int m = 1; m <= 12; m++) ChoixMois.Items.Add(new ComboBoxItem { Content = Majuscule(Fr.DateTimeFormat.GetMonthName(m)), Tag = m });
        foreach (int a in ChoixDate.Annees(Aujourdhui, Donnees.Retraite)) ChoixAnnee.Items.Add(a);

        if (Donnees.Retraite is { } r)
        {
            ChoixJour.SelectedItem = r.Day;
            ChoixMois.SelectedIndex = r.Month - 1;
            ChoixAnnee.SelectedItem = r.Year;
        }
    }

    private void Date_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_chargement || ChoixJour.SelectedItem is not int jour || ChoixMois.SelectedItem is not ComboBoxItem { Tag: int mois }
            || ChoixAnnee.SelectedItem is not int annee)
            return;

        var date = ChoixDate.Composer(jour, mois, annee);
        if (date.Day != jour)
        {
            // 31 juin → 30 juin : on affiche le jour réellement retenu.
            _chargement = true;
            ChoixJour.SelectedItem = date.Day;
            _chargement = false;
        }
        Donnees.Retraite = date;
        Modifie();
    }

    // ---------- Apparence : trois listes ----------

    private static void Remplir<T>(ComboBox liste, T actuel, params (T Valeur, string Texte)[] choix) where T : struct, Enum
    {
        foreach (var (valeur, texte) in choix)
        {
            var element = new ComboBoxItem { Content = texte, Tag = valeur };
            liste.Items.Add(element);
            if (EqualityComparer<T>.Default.Equals(valeur, actuel)) liste.SelectedItem = element;
        }
    }

    private static object Valeur(ComboBox liste) => ((ComboBoxItem)liste.SelectedItem).Tag;

    private void Apparence(Action<Core.Apparence> changer)
    {
        if (_chargement) return;
        changer(Donnees.Apparence);
        Modifie();
    }

    // ---------- Mise à jour de l'affichage ----------

    private void MettreAJour()
    {
        bool dateConnue = Donnees.Retraite is not null;
        Calendrier.Calendrier = dateConnue ? Donnees.Calendrier() : null;
        Calendrier.Retraite = Donnees.Retraite;
        Calendrier.Aujourdhui = Aujourdhui;
        Calendrier.IsEnabled = CarteLot.IsEnabled = dateConnue;
        Calendrier.Construire();
        AideCalendrier.Text = dateConnue
            ? "Cliquez sur un jour où vous ne travaillez pas (vacances, congé…). Cliquez de nouveau pour annuler."
            : "Indiquez d'abord votre premier jour de retraite.";

        if (Donnees.Retraite is not { } retraite)
            Resume.Text = "Indiquez votre date de retraite";
        else if (Aujourdhui >= retraite)
            Resume.Text = $"Bonne retraite, {Donnees.Prenom} !";
        else
        {
            int n = Compteur.JoursRestants(Donnees.Calendrier(), Aujourdhui, retraite);
            Resume.Text = n == 1 ? "Il reste 1 jour travaillé" : $"Il reste {Libelles.Nombre(n)} jours travaillés";
        }
    }

    private void Modifie()
    {
        if (_chargement) return;
        App.Current.Rafraichir();
        MettreAJour();
    }

    private void Prenom_Changed(object sender, TextChangedEventArgs e)
    {
        if (_chargement) return;
        var prenom = ChampPrenom.Text.Trim();
        if (prenom.Length == 0) return;
        Donnees.Prenom = prenom;
        Modifie();
    }

    // ---------- Un jour chaque semaine ----------

    private DayOfWeek? JourChoisi => ChoixJourSemaine.SelectedItem is ComboBoxItem { Tag: DayOfWeek j } ? j : null;
    private static string Pluriel(DayOfWeek j) => Fr.DateTimeFormat.GetDayName(j) + "s";

    private void NeTravaillePlus_Click(object sender, RoutedEventArgs e)
    {
        if (Donnees.Retraite is not { } retraite || JourChoisi is not { } jour) return;
        // Aperçu sur une copie : le nombre annoncé est exactement celui qui sera appliqué.
        int n = Donnees.Calendrier().Copie().NeTravaillePlus(jour, Aujourdhui, retraite);
        if (n == 0)
        {
            ResultatLot.Text = $"Les {Pluriel(jour)} sont déjà tous marqués «\u00A0non travaillé\u00A0».";
            return;
        }
        if (!Confirmation.Demander(this, $"Vous ne travaillez plus le {Fr.DateTimeFormat.GetDayName(jour)} ?",
                $"{Majuscule(Libelles.JoursSemaine(n, jour))} d'ici la retraite {(n > 1 ? "seront marqués" : "sera marqué")} «\u00A0non travaillé\u00A0».\n\n" +
                "Vous pourrez ensuite remettre un jour d'un clic dans le calendrier.",
                "Oui, appliquer"))
            return;

        int changes = Donnees.Calendrier().NeTravaillePlus(jour, Aujourdhui, retraite);
        ResultatLot.Text = $"C'est fait : {Libelles.JoursSemaine(changes, jour)} en moins à travailler.";
        Modifie();
    }

    private void TravailleDeNouveau_Click(object sender, RoutedEventArgs e)
    {
        if (Donnees.Retraite is not { } retraite || JourChoisi is not { } jour) return;
        int n = Donnees.Calendrier().Copie().TravailleDeNouveau(jour, Aujourdhui, retraite);
        if (n == 0)
        {
            ResultatLot.Text = $"Les {Pluriel(jour)} sont déjà travaillés (hors jours fériés).";
            return;
        }
        if (!Confirmation.Demander(this, $"Vous travaillez le {Fr.DateTimeFormat.GetDayName(jour)} ?",
                $"{Majuscule(Libelles.JoursSemaine(n, jour))} d'ici la retraite {(n > 1 ? "redeviendront travaillés" : "redeviendra travaillé")}.\n\n" +
                "Les changements faits à la main sur ces jours seront effacés.",
                "Oui, appliquer"))
            return;

        int changes = Donnees.Calendrier().TravailleDeNouveau(jour, Aujourdhui, retraite);
        ResultatLot.Text = $"C'est fait : {Libelles.JoursSemaine(changes, jour)} de nouveau à travailler.";
        Modifie();
    }

    private static string Majuscule(string texte) => texte.Length == 0 ? texte : char.ToUpper(texte[0], Fr) + texte[1..];

    // ---------- Bas de fenêtre ----------

    private void Desinstaller_Click(object sender, RoutedEventArgs e)
    {
        if (Confirmation.Demander(this, "Désinstaller le compteur ?",
                "Le compteur sera retiré de cet ordinateur, avec tous ses réglages et vos jours de vacances.",
                "Oui, désinstaller"))
            App.Current.Desinstaller();
    }

    private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
}
