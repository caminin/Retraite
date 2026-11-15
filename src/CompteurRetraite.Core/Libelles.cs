using System.Globalization;

namespace CompteurRetraite.Core;

public static class Libelles
{
    private static readonly CultureInfo Fr = new("fr-FR");

    /// <summary>« 0 mercredi », « 1 mercredi », « 245 mercredis » (en français, 0 et 1 sont au singulier).</summary>
    public static string JoursSemaine(int n, DayOfWeek jour)
    {
        string nom = Fr.DateTimeFormat.GetDayName(jour);
        return $"{n.ToString("N0", Fr)} {nom}{(n > 1 ? "s" : "")}";
    }
}
