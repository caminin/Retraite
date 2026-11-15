using static CompteurRetraite.App.Bureau.Win32;

namespace CompteurRetraite.App.Bureau;

/// <summary>
/// Place le compteur juste au-dessus du bureau (Progman), sous toutes les autres fenêtres.
///
/// Pourquoi pas « à l'intérieur » du bureau (technique WorkerW de Lively/Wallpaper Engine) ?
/// Testé sur Windows 11 25H2 : depuis 24H2, Progman est composé (WS_EX_NOREDIRECTIONBITMAP) et une
/// fenêtre transparente (WS_EX_LAYERED) placée dans Progman ou dans son WorkerW n'est jamais dessinée.
/// Une fenêtre de premier niveau garde sa transparence ; elle est traversée par les clics, n'apparaît ni
/// dans la barre des tâches ni dans Alt+Tab, et les fenêtres ouvertes passent toujours devant.
/// </summary>
internal static class AttacheBureau
{
    /// <summary>Place la fenêtre juste au-dessus du bureau. Renvoie toujours IntPtr.Zero (pas de fenêtre parente).</summary>
    public static IntPtr Attacher(IntPtr hwnd)
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            return IntPtr.Zero;
        }

        // « insertAfter = X » place la fenêtre juste sous X : on vise la fenêtre située juste au-dessus du bureau.
        var auDessus = GetWindow(progman, GW_HWNDPREV);
        if (auDessus == hwnd) return IntPtr.Zero; // déjà à la bonne place
        SetWindowPos(hwnd, auDessus == IntPtr.Zero ? HWND_BOTTOM : auDessus, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        return IntPtr.Zero;
    }
}
