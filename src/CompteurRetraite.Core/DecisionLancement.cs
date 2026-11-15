namespace CompteurRetraite.Core;

public enum ActionLancement { Installer, RemplacerEtRelancer, SignalerInstanceEnCours, Demarrer }

public static class DecisionLancement
{
    /// <summary>Que faire quand l'exe est lancé ? Les chemins sont complets.</summary>
    public static ActionLancement Decider(string exeLance, string exeInstalle, bool installeExiste,
                                          Version versionLancee, Version? versionInstallee, bool instanceEnCours)
    {
        bool depuisInstalle = string.Equals(exeLance.Trim(), exeInstalle.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!depuisInstalle)
        {
            if (!installeExiste) return ActionLancement.Installer;
            if (versionInstallee is null || versionLancee > versionInstallee) return ActionLancement.RemplacerEtRelancer;
        }
        return instanceEnCours ? ActionLancement.SignalerInstanceEnCours : ActionLancement.Demarrer;
    }
}
