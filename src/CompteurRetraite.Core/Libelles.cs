using System.Globalization;

namespace CompteurRetraite.Core;

public static class Libelles
{
    private static readonly CultureInfo Fr = new("fr-FR");

    // .NET sépare les milliers français par une espace fine (U+202F), mal rendue par certaines polices Windows :
    // on utilise l'espace insécable classique (U+00A0).
    private static readonly NumberFormatInfo FormatNombres = new()
    {
        NumberGroupSeparator = " ",
        NumberGroupSizes = [3],
        NumberDecimalDigits = 0,
    };

    /// <summary>« 929 », « 1 163 » (espace insécable).</summary>
    public static string Nombre(int n) => n.ToString("N0", FormatNombres);

    /// <summary>« 0 mercredi », « 1 mercredi », « 245 mercredis » (en français, 0 et 1 sont au singulier).</summary>
    public static string JoursSemaine(int n, DayOfWeek jour)
    {
        string nom = Fr.DateTimeFormat.GetDayName(jour);
        return $"{Nombre(n)} {nom}{(n > 1 ? "s" : "")}";
    }
}
