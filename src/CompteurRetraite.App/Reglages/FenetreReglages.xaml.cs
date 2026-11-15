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

    private readonly bool _chargement;
    private static Donnees Donnees => App.Current.Donnees;
    private static DateOnly Aujourdhui => DateOnly.FromDateTime(DateTime.Now);

    public FenetreReglages(bool bienvenue)
    {
        _chargement = true;
        InitializeComponent();
        Language = XmlLanguage.GetLanguage("fr-FR");

        if (bienvenue)
        {
            Titre.Text = $"Bienvenue, {Donnees.Prenom} !";
            SousTitre.Text = "Indiquez votre premier jour de retraite : le compteur s'affichera sur votre bureau, " +
                             "derrière vos icônes, sans toucher à votre fond d'écran.";
        }

        ChampPrenom.Text = Donnees.Prenom;
        ChampRetraite.SelectedDate = Donnees.Retraite?.ToDateTime(TimeOnly.MinValue);
        ChampRetraite.DisplayDateStart = DateTime.Today.AddYears(-1);

        foreach (var j in JoursOuvres)
        {
            ChoixJourOff.Items.Add(new ComboBoxItem { Content = Fr.DateTimeFormat.GetDayName(j), Tag = j });
            ChoixJourOn.Items.Add(new ComboBoxItem { Content = Fr.DateTimeFormat.GetDayName(j), Tag = j });
        }
        ChoixJourOff.SelectedIndex = 2; // mercredi
        ChoixJourOn.SelectedIndex = 2;

        Pastilles(ChoixCoin, "coin", Donnees.Apparence.Coin,
            (Coin.BasDroite, "En bas à droite"), (Coin.BasGauche, "En bas à gauche"),
            (Coin.HautDroite, "En haut à droite"), (Coin.HautGauche, "En haut à gauche"));
        Pastilles(ChoixTaille, "taille", Donnees.Apparence.Taille,
            (Taille.Petit, "Petit"), (Taille.Moyen, "Moyen"), (Taille.Grand, "Grand"));
        Pastilles(ChoixTheme, "theme", Donnees.Apparence.Theme,
            (Theme.Clair, "Clair (fond d'écran foncé)"), (Theme.Fonce, "Foncé (fond d'écran clair)"));

        Calendrier.Change += Modifie;
        _chargement = false;
        MettreAJour();
    }

    private void Pastilles<T>(Panel panneau, string groupe, T actuel, params (T Valeur, string Texte)[] choix) where T : struct, Enum
    {
        foreach (var (valeur, texte) in choix)
        {
            var bouton = new RadioButton
            {
                Content = texte, GroupName = groupe, Style = (Style)FindResource("Pastille"),
                IsChecked = EqualityComparer<T>.Default.Equals(valeur, actuel),
            };
            bouton.Checked += (_, _) =>
            {
                if (_chargement) return;
                switch (valeur)
                {
                    case Coin c: Donnees.Apparence.Coin = c; break;
                    case Taille t: Donnees.Apparence.Taille = t; break;
                    case Theme th: Donnees.Apparence.Theme = th; break;
                }
                Modifie();
            };
            panneau.Children.Add(bouton);
        }
    }

    /// <summary>Met à jour le résumé, le calendrier et ce qui dépend de la date de retraite.</summary>
    private void MettreAJour()
    {
        bool dateConnue = Donnees.Retraite is not null;
        Calendrier.Calendrier = dateConnue ? Donnees.Calendrier() : null;
        Calendrier.Retraite = Donnees.Retraite;
        Calendrier.Aujourdhui = Aujourdhui;
        Calendrier.IsEnabled = CarteLot.IsEnabled = dateConnue;
        Calendrier.Construire();
        AideCalendrier.Text = dateConnue
            ? "Cliquez sur un jour pour indiquer que vous ne travaillez pas (vacances, congé…). Cliquez de nouveau pour annuler."
            : "Indiquez d'abord votre date de retraite.";

        if (Donnees.Retraite is not { } retraite)
            Resume.Text = "Indiquez votre premier jour de retraite";
        else if (Aujourdhui >= retraite)
            Resume.Text = $"Bonne retraite, {Donnees.Prenom} !";
        else
        {
            int n = Compteur.JoursRestants(Donnees.Calendrier(), Aujourdhui, retraite);
            Resume.Text = n == 1 ? "Il reste 1 jour travaillé" : $"Il reste {n.ToString("N0", Fr)} jours travaillés";
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

    private void Retraite_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (_chargement || ChampRetraite.SelectedDate is not { } date) return;
        Donnees.Retraite = DateOnly.FromDateTime(date);
        Modifie();
    }

    private static string Pluriel(DayOfWeek j) => Fr.DateTimeFormat.GetDayName(j) + "s";

    private void NeTravaillePlus_Click(object sender, RoutedEventArgs e)
    {
        if (Donnees.Retraite is not { } retraite || ChoixJourOff.SelectedItem is not ComboBoxItem { Tag: DayOfWeek jour }) return;
        // Aperçu sur une copie : le nombre annoncé est exactement celui qui sera appliqué.
        int n = Donnees.Calendrier().Copie().NeTravaillePlus(jour, Aujourdhui, retraite);
        if (n == 0)
        {
            ResultatLot.Text = $"Les {Pluriel(jour)} à venir sont déjà tous non travaillés.";
            return;
        }
        var reponse = MessageBox.Show(this,
            $"{Majuscule(Libelles.JoursSemaine(n, jour))} à venir {(n > 1 ? "seront marqués" : "sera marqué")} comme non travaillé{(n > 1 ? "s" : "")}, jusqu'à la retraite.\n\n" +
            "Vous pourrez ensuite remettre un jour travaillé en cliquant dessus dans le calendrier.",
            "Confirmer", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (reponse != MessageBoxResult.OK) return;

        int changes = Donnees.Calendrier().NeTravaillePlus(jour, Aujourdhui, retraite);
        ResultatLot.Text = $"C'est fait : {Libelles.JoursSemaine(changes, jour)} en moins à travailler.";
        Modifie();
    }

    private void TravailleDeNouveau_Click(object sender, RoutedEventArgs e)
    {
        if (Donnees.Retraite is not { } retraite || ChoixJourOn.SelectedItem is not ComboBoxItem { Tag: DayOfWeek jour }) return;
        int n = Donnees.Calendrier().Copie().TravailleDeNouveau(jour, Aujourdhui, retraite);
        if (n == 0)
        {
            ResultatLot.Text = $"Les {Pluriel(jour)} à venir sont déjà travaillés (hors jours fériés).";
            return;
        }
        var reponse = MessageBox.Show(this,
            $"{Majuscule(Libelles.JoursSemaine(n, jour))} à venir {(n > 1 ? "redeviendront travaillés" : "redeviendra travaillé")}.\n\n" +
            "Les changements faits à la main sur ces jours seront effacés.",
            "Confirmer", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (reponse != MessageBoxResult.OK) return;

        int changes = Donnees.Calendrier().TravailleDeNouveau(jour, Aujourdhui, retraite);
        ResultatLot.Text = $"C'est fait : {Libelles.JoursSemaine(changes, jour)} de nouveau à travailler.";
        Modifie();
    }

    private static string Majuscule(string texte) => texte.Length == 0 ? texte : char.ToUpper(texte[0], Fr) + texte[1..];

    private void Desinstaller_Click(object sender, RoutedEventArgs e)
    {
        var reponse = MessageBox.Show(this,
            "Le compteur va être retiré de cet ordinateur, avec tous ses réglages.\n\nContinuer ?",
            "Désinstaller le compteur", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (reponse == MessageBoxResult.Yes) App.Current.Desinstaller();
    }

    private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
}
