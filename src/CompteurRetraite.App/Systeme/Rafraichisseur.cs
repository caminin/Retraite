using System.Windows.Threading;
using CompteurRetraite.Core;
using Microsoft.Win32;

namespace CompteurRetraite.App.Systeme;

/// <summary>Déclenche un rafraîchissement à minuit, en sortie de veille, au changement d'heure ou d'écran.</summary>
internal sealed class Rafraichisseur : IDisposable
{
    private readonly Dispatcher _ui;
    private readonly Action _rafraichir;
    private readonly Action _rattacher;
    private readonly DispatcherTimer _minuit;
    private readonly DispatcherTimer _garde;
    private DateOnly _jourAffiche = DateOnly.FromDateTime(DateTime.Now);

    public Rafraichisseur(Dispatcher ui, Action rafraichir, Action rattacher)
    {
        _ui = ui;
        _rafraichir = rafraichir;
        _rattacher = rattacher;

        _minuit = new DispatcherTimer(DispatcherPriority.Normal, ui);
        _minuit.Tick += (_, _) => Rafraichir();
        // Filet de sécurité : les minuteries peuvent dériver pendant la veille.
        _garde = new DispatcherTimer(TimeSpan.FromMinutes(5), DispatcherPriority.Background, (_, _) =>
        {
            if (DateOnly.FromDateTime(DateTime.Now) != _jourAffiche) Rafraichir();
        }, ui);

        SystemEvents.PowerModeChanged += SurPuissance;
        SystemEvents.TimeChanged += SurHeure;
        SystemEvents.DisplaySettingsChanged += SurEcran;
        Armer();
        _garde.Start();
    }

    private void Rafraichir()
    {
        _jourAffiche = DateOnly.FromDateTime(DateTime.Now);
        _rafraichir();
        Armer();
    }

    private void Armer()
    {
        _minuit.Stop();
        _minuit.Interval = Horloge.JusquAuProchainMinuit(DateTime.Now);
        _minuit.Start();
    }

    private void SurPuissance(object? s, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) _ui.BeginInvoke(() => { _rattacher(); Rafraichir(); });
    }

    private void SurHeure(object? s, EventArgs e) => _ui.BeginInvoke(Rafraichir);

    private void SurEcran(object? s, EventArgs e) => _ui.BeginInvoke(() => { _rattacher(); Rafraichir(); });

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= SurPuissance;
        SystemEvents.TimeChanged -= SurHeure;
        SystemEvents.DisplaySettingsChanged -= SurEcran;
        _minuit.Stop();
        _garde.Stop();
    }
}
