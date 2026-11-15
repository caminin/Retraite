using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CompteurRetraite.Core;
using static CompteurRetraite.App.Bureau.Win32;

namespace CompteurRetraite.App.Bureau;

/// <summary>Le compteur dessiné sur le bureau : transparent, traversé par les clics, derrière les icônes.</summary>
public partial class FenetreCompteur : Window
{
    private static readonly CultureInfo Fr = new("fr-FR");
    private IntPtr _parent;
    private Apparence _apparence = new();

    public FenetreCompteur()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = Handle;
            long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(hwnd, GWL_EXSTYLE,
                new IntPtr(style | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
            Rattacher();
        };
        SizeChanged += (_, _) => Positionner();
    }

    private IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Rattacher()
    {
        if (Handle == IntPtr.Zero) return;
        _parent = AttacheBureau.Attacher(Handle);
        Positionner();
    }

    public void Afficher(EtatAffichage e, Apparence a)
    {
        _apparence = a;
        double taille = a.Taille switch { Taille.Petit => 72, Taille.Grand => 150, _ => 110 };
        bool clair = a.Theme == Theme.Clair;
        var encre = new SolidColorBrush(clair ? Color.FromArgb(230, 255, 255, 255) : Color.FromRgb(0x1E, 0x26, 0x33));
        encre.Freeze();

        Ombre.Color = clair ? Colors.Black : Colors.White;
        Ombre.Opacity = clair ? 0.35 : 0.25;

        var alignement = a.Coin is Coin.BasDroite or Coin.HautDroite ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        var alignTexte = alignement == HorizontalAlignment.Right ? TextAlignment.Right : TextAlignment.Left;
        foreach (FrameworkElement el in Pile.Children) el.HorizontalAlignment = alignement;

        // Hors décompte (date à saisir, retraite atteinte), un message remplace le chiffre.
        bool compte = e.Mode == Mode.Compte;
        TexteChiffre.Visibility = TexteLibelle.Visibility = ZoneProgression.Visibility =
            compte ? Visibility.Visible : Visibility.Collapsed;
        TexteMessage.Visibility = compte ? Visibility.Collapsed : Visibility.Visible;
        TexteMessage.Text = e.Message;
        TexteMessage.TextAlignment = alignTexte;
        TexteMessage.Foreground = encre;
        TexteMessage.MaxWidth = taille * 5.5;
        TexteMessage.FontSize = taille * 0.45;

        TexteChiffre.Text = e.Chiffre;
        TexteChiffre.FontSize = taille;
        TexteChiffre.LineHeight = taille * 1.05;
        TexteChiffre.Foreground = encre;

        TexteLibelle.Text = e.Libelle.ToUpper(Fr);
        TexteLibelle.FontSize = taille * 0.16;
        TexteLibelle.Foreground = encre;

        Barre.Width = taille * 2.6;
        ColFait.Width = new GridLength(e.Pourcentage, GridUnitType.Star);
        ColReste.Width = new GridLength(100 - e.Pourcentage, GridUnitType.Star);
        Piste.Background = new SolidColorBrush(clair ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(60, 0x1E, 0x26, 0x33));
        Remplissage.Background = encre;
        TextePourcentage.Text = $"{e.Pourcentage} %";
        TextePourcentage.FontSize = taille * 0.16;
        TextePourcentage.Foreground = encre;

        UpdateLayout();
        Positionner();
    }

    /// <summary>Place la fenêtre dans le coin choisi de l'écran principal, en pixels physiques.</summary>
    public void Positionner()
    {
        var hwnd = Handle;
        if (hwnd == IntPtr.Zero || ActualWidth <= 0) return;

        var dpi = VisualTreeHelper.GetDpi(this);
        int largeur = (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX);
        int hauteur = (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY);
        int marge = (int)(48 * dpi.DpiScaleX);
        var zone = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea
                   ?? new System.Drawing.Rectangle(0, 0, 1920, 1040);

        bool droite = _apparence.Coin is Coin.BasDroite or Coin.HautDroite;
        bool bas = _apparence.Coin is Coin.BasDroite or Coin.BasGauche;
        var p = new POINT
        {
            X = droite ? zone.Right - largeur - marge : zone.Left + marge,
            Y = bas ? zone.Bottom - hauteur - marge : zone.Top + marge,
        };
        if (_parent != IntPtr.Zero) MapWindowPoints(IntPtr.Zero, _parent, ref p, 1);
        SetWindowPos(hwnd, IntPtr.Zero, p.X, p.Y, largeur, hauteur, SWP_NOZORDER | SWP_NOACTIVATE);
    }
}
