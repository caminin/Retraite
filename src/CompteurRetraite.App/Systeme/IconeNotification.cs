using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CompteurRetraite.App.Systeme;

/// <summary>Icône près de l'horloge, avec son menu.</summary>
internal sealed class IconeNotification : IDisposable
{
    private readonly NotifyIcon _icone;
    private readonly ToolStripMenuItem _masquer;
    private readonly Icon _image;

    public event Action? ReglagesDemandes;
    public event Action? MasquageDemande;
    public event Action? QuitterDemande;

    public IconeNotification()
    {
        _image = Dessiner();
        _masquer = new ToolStripMenuItem("Masquer le compteur", null, (_, _) => MasquageDemande?.Invoke());
        var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 10f) };
        menu.Items.Add(new ToolStripMenuItem("Réglages…", null, (_, _) => ReglagesDemandes?.Invoke()) { Font = new Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold) });
        menu.Items.Add(_masquer);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Quitter", null, (_, _) => QuitterDemande?.Invoke()));

        _icone = new NotifyIcon { Icon = _image, Text = "Compteur retraite", ContextMenuStrip = menu, Visible = true };
        _icone.DoubleClick += (_, _) => ReglagesDemandes?.Invoke();
    }

    public void MettreAJour(string infobulle, bool masque)
    {
        _icone.Text = infobulle.Length > 63 ? infobulle[..62] + "…" : infobulle;
        _masquer.Text = masque ? "Afficher le compteur" : "Masquer le compteur";
    }

    /// <summary>Un carré bleu arrondi avec un petit sablier blanc.</summary>
    private static Icon Dessiner()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fond = new SolidBrush(Color.FromArgb(0x1F, 0x8B, 0xFF));
            using var chemin = new GraphicsPath();
            chemin.AddArc(0, 0, 12, 12, 180, 90);
            chemin.AddArc(19, 0, 12, 12, 270, 90);
            chemin.AddArc(19, 19, 12, 12, 0, 90);
            chemin.AddArc(0, 19, 12, 12, 90, 90);
            chemin.CloseFigure();
            g.FillPath(fond, chemin);
            using var blanc = new SolidBrush(Color.White);
            g.FillPolygon(blanc, new[] { new Point(9, 7), new Point(23, 7), new Point(16, 16) });
            g.FillPolygon(blanc, new[] { new Point(16, 16), new Point(23, 25), new Point(9, 25) });
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _icone.Visible = false;
        _icone.Dispose();
        _image.Dispose();
    }
}
