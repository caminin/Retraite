namespace CompteurRetraite.Core;

public static class Horloge
{
    private static readonly TimeSpan Marge = TimeSpan.FromSeconds(2);

    /// <summary>Délai avant le prochain minuit (strictement après maintenant), plus 2 s de marge.</summary>
    public static TimeSpan JusquAuProchainMinuit(DateTime maintenant)
        => maintenant.Date.AddDays(1) - maintenant + Marge;
}
