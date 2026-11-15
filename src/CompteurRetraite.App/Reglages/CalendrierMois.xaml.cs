using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CompteurRetraite.Core;

namespace CompteurRetraite.App.Reglages;

/// <summary>Calendrier mensuel cliquable : un clic inverse travaillé / non travaillé.</summary>
public partial class CalendrierMois : UserControl
{
    private static readonly CultureInfo Fr = new("fr-FR");
    private DateOnly _mois;

    public CalendrierTravail? Calendrier { get; set; }
    public DateOnly Aujourdhui { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly? Retraite { get; set; }

    /// <summary>Un jour a été modifié.</summary>
    public event Action? Change;

    public CalendrierMois()
    {
        InitializeComponent();
        _mois = new DateOnly(Aujourdhui.Year, Aujourdhui.Month, 1);
        foreach (var nom in new[] { "L", "M", "M", "J", "V", "S", "D" })
            EnTetes.Children.Add(new TextBlock
            {
                Text = nom, FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Pinceau("Discret"),
            });
        ConstruireLegende();
        Loaded += (_, _) => Construire();
    }

    public void Construire()
    {
        string titre = _mois.ToString("MMMM yyyy", Fr);
        TitreMois.Text = char.ToUpper(titre[0], Fr) + titre[1..];
        BoutonAujourdhui.IsEnabled = _mois != new DateOnly(Aujourdhui.Year, Aujourdhui.Month, 1);

        Grille.Children.Clear();
        int decalage = ((int)_mois.DayOfWeek + 6) % 7; // lundi = 0
        var premier = _mois.AddDays(-decalage);
        for (int i = 0; i < 42; i++)
        {
            var jour = premier.AddDays(i);
            Grille.Children.Add(jour.Month == _mois.Month ? CaseJour(jour) : new Border());
        }
    }

    private UIElement CaseJour(DateOnly jour)
    {
        var type = Calendrier?.Statut(jour) ?? TypeJour.Travaille;
        bool passe = jour < Aujourdhui;
        bool apresRetraite = Retraite is { } r && jour >= r;
        bool cliquable = Calendrier is not null && type != TypeJour.WeekEnd && !passe && !apresRetraite;

        (Brush fond, Brush encre) = type switch
        {
            TypeJour.WeekEnd => (Pinceau("JourWeekEnd"), Pinceau("JourWeekEndEncre")),
            TypeJour.Ferie => (Pinceau("JourFerie"), Pinceau("JourFerieEncre")),
            TypeJour.NonTravailleLot or TypeJour.NonTravailleManuel => (Pinceau("JourNonTravaille"), Pinceau("JourNonTravailleEncre")),
            _ => (Pinceau("JourTravaille"), Pinceau("Encre")),
        };

        var contenu = new TextBlock
        {
            Text = jour.Day.ToString(Fr), FontSize = 17, Foreground = encre,
            FontWeight = jour == Aujourdhui ? FontWeights.Bold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        var cadre = new Border
        {
            Background = fond, CornerRadius = new CornerRadius(8), Margin = new Thickness(3), MinHeight = 46,
            BorderBrush = jour == Aujourdhui ? Pinceau("Accent") : Pinceau("Trait"),
            BorderThickness = new Thickness(jour == Aujourdhui ? 3 : 1),
            Opacity = passe || apresRetraite ? 0.45 : 1,
            Child = contenu,
            ToolTip = Infobulle(jour, type, apresRetraite),
        };
        if (!cliquable) return cadre;

        var bouton = new Button { Content = cadre, Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(0) };
        bouton.Template = ModeleSimple();
        bouton.Click += (_, _) =>
        {
            if (Calendrier?.Basculer(jour) != true) return;
            Construire();
            Change?.Invoke();
        };
        return bouton;
    }

    private static string Infobulle(DateOnly jour, TypeJour type, bool apresRetraite)
    {
        string date = jour.ToString("dddd d MMMM yyyy", Fr);
        if (apresRetraite) return $"{date} : retraite !";
        return type switch
        {
            TypeJour.WeekEnd => $"{date} : week-end",
            TypeJour.Ferie => $"{date} : jour férié (cliquez s'il est travaillé)",
            TypeJour.NonTravailleLot or TypeJour.NonTravailleManuel => $"{date} : non travaillé (cliquez pour le remettre travaillé)",
            _ => $"{date} : travaillé (cliquez s'il ne l'est pas)",
        };
    }

    private static ControlTemplate ModeleSimple()
    {
        var modele = new ControlTemplate(typeof(Button));
        modele.VisualTree = new FrameworkElementFactory(typeof(ContentPresenter));
        return modele;
    }

    private void ConstruireLegende()
    {
        foreach (var (cle, texte) in new[]
                 {
                     ("JourTravaille", "Travaillé"), ("JourNonTravaille", "Non travaillé / vacances"),
                     ("JourFerie", "Férié"), ("JourWeekEnd", "Week-end"),
                 })
        {
            var pastille = new Border
            {
                Width = 18, Height = 18, CornerRadius = new CornerRadius(4), Background = Pinceau(cle),
                BorderBrush = Pinceau("Trait"), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 6, 0),
            };
            var ligne = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 18, 6) };
            ligne.Children.Add(pastille);
            ligne.Children.Add(new TextBlock { Text = texte, FontSize = 14, Foreground = Pinceau("Discret"), VerticalAlignment = VerticalAlignment.Center });
            Legende.Children.Add(ligne);
        }
    }

    private static Brush Pinceau(string cle) => (Brush)System.Windows.Application.Current.FindResource(cle);

    private void Precedent_Click(object sender, RoutedEventArgs e) { _mois = _mois.AddMonths(-1); Construire(); }
    private void Suivant_Click(object sender, RoutedEventArgs e) { _mois = _mois.AddMonths(1); Construire(); }
    private void Aujourdhui_Click(object sender, RoutedEventArgs e) { _mois = new DateOnly(Aujourdhui.Year, Aujourdhui.Month, 1); Construire(); }
}
