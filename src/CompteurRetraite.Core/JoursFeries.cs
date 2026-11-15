using System.Collections.Concurrent;

namespace CompteurRetraite.Core;

/// <summary>Jours fériés de France métropolitaine.</summary>
public static class JoursFeries
{
    private static readonly ConcurrentDictionary<int, IReadOnlySet<DateOnly>> Cache = new();

    /// <summary>Dimanche de Pâques (algorithme grégorien anonyme, Meeus/Jones/Butcher).</summary>
    public static DateOnly Paques(int annee)
    {
        int a = annee % 19, b = annee / 100, c = annee % 100;
        int d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int mois = (h + l - 7 * m + 114) / 31;
        int jour = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(annee, mois, jour);
    }

    public static IReadOnlySet<DateOnly> PourAnnee(int annee) => Cache.GetOrAdd(annee, Calculer);

    public static bool EstFerie(DateOnly jour) => PourAnnee(jour.Year).Contains(jour);

    private static IReadOnlySet<DateOnly> Calculer(int annee)
    {
        var paques = Paques(annee);
        return new HashSet<DateOnly>
        {
            new(annee, 1, 1),
            paques.AddDays(1),   // lundi de Pâques
            new(annee, 5, 1),
            new(annee, 5, 8),
            paques.AddDays(39),  // Ascension
            paques.AddDays(50),  // lundi de Pentecôte
            new(annee, 7, 14),
            new(annee, 8, 15),
            new(annee, 11, 1),
            new(annee, 11, 11),
            new(annee, 12, 25),
        };
    }
}
