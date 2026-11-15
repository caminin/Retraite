using System.Windows;

namespace CompteurRetraite.App.Reglages;

/// <summary>Boîte de confirmation en gros caractères (les boîtes standard de Windows sont trop petites).</summary>
public partial class Confirmation : Window
{
    private Confirmation(string titre, string message, string oui)
    {
        InitializeComponent();
        TexteTitre.Text = titre;
        TexteMessage.Text = message;
        BoutonOui.Content = oui;
    }

    public static bool Demander(Window proprietaire, string titre, string message, string oui)
    {
        var boite = new Confirmation(titre, message, oui) { Owner = proprietaire };
        return boite.ShowDialog() == true;
    }

    private void Oui_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Non_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
