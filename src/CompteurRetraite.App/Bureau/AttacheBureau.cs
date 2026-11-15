using static CompteurRetraite.App.Bureau.Win32;

namespace CompteurRetraite.App.Bureau;

/// <summary>
/// Place une fenêtre entre le fond d'écran et les icônes du bureau (technique « WorkerW »).
/// Windows 11 24H2+ : le WorkerW du fond d'écran est un enfant de Progman.
/// Avant 24H2 : un WorkerW de premier niveau, juste derrière la fenêtre qui contient les icônes.
/// </summary>
internal static class AttacheBureau
{
    /// <summary>Renvoie la fenêtre parente choisie, ou IntPtr.Zero si on a dû se replier sur « tout en bas ».</summary>
    public static IntPtr Attacher(IntPtr hwnd)
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return Replier(hwnd);

        // Demande à l'Explorateur de créer la couche WorkerW derrière les icônes.
        SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(1), SMTO_NORMAL, 1000, out _);
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);

        // 24H2 et suivantes : après le message ci-dessus, les icônes (SHELLDLL_DefView) restent sous Progman,
        // à côté du WorkerW du fond d'écran. Avant 24H2, elles sont déplacées dans un WorkerW séparé.
        var workerEnfant = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        var iconesSousProgman = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (workerEnfant != IntPtr.Zero || iconesSousProgman != IntPtr.Zero)
        {
            SetParent(hwnd, progman);
            // Placée juste sous la vue des icônes, donc au-dessus du fond d'écran.
            // Pas de SWP_SHOWWINDOW : c'est WPF qui décide si le compteur est visible (option « Masquer »).
            SetWindowPos(hwnd, iconesSousProgman, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            return progman;
        }

        // Versions précédentes.
        IntPtr workerFond = IntPtr.Zero;
        EnumWindows((haut, _) =>
        {
            if (FindWindowEx(haut, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
            {
                workerFond = FindWindowEx(IntPtr.Zero, haut, "WorkerW", null);
                return false;
            }
            return true;
        }, IntPtr.Zero);

        if (workerFond != IntPtr.Zero)
        {
            SetParent(hwnd, workerFond);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            return workerFond;
        }

        return Replier(hwnd);
    }

    private static IntPtr Replier(IntPtr hwnd)
    {
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        return IntPtr.Zero;
    }
}
