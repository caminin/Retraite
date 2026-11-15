namespace CompteurRetraite.Core;

/// <summary>
/// Statut travaillé / non travaillé de chaque jour.
/// Priorité : correction manuelle > règle en lot > férié > week-end > travaillé.
/// Les week-ends ne sont jamais travaillés et ignorent corrections et règles.
/// </summary>
public sealed class CalendrierTravail(List<RegleLot> regles, Dictionary<DateOnly, bool> corrections)
{
    public static bool EstWeekEnd(DateOnly jour) =>
        jour.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public TypeJour Statut(DateOnly jour)
    {
        if (EstWeekEnd(jour)) return TypeJour.WeekEnd;
        if (corrections.TryGetValue(jour, out bool travaille))
            return travaille ? TypeJour.TravailleManuel : TypeJour.NonTravailleManuel;
        return StatutSansCorrection(jour);
    }

    /// <summary>Copie indépendante, pour prévisualiser une action sans rien modifier.</summary>
    public CalendrierTravail Copie() => new(new List<RegleLot>(regles), new Dictionary<DateOnly, bool>(corrections));

    public bool EstTravaille(DateOnly jour) => Statut(jour) is TypeJour.Travaille or TypeJour.TravailleManuel;

    /// <summary>Inverse le statut d'un jour. Renvoie false (sans rien changer) pour un week-end.</summary>
    public bool Basculer(DateOnly jour)
    {
        if (EstWeekEnd(jour)) return false;
        bool nouveau = !EstTravaille(jour);
        bool parDefaut = StatutSansCorrection(jour) == TypeJour.Travaille;
        if (nouveau == parDefaut) corrections.Remove(jour);
        else corrections[jour] = nouveau;
        return true;
    }

    /// <summary>« Je ne travaille plus le <paramref name="jour"/> » à partir d'aujourd'hui. Renvoie la baisse du nombre de jours travaillés.</summary>
    public int NeTravaillePlus(DayOfWeek jour, DateOnly aujourdhui, DateOnly retraite)
    {
        int avant = CompterTravailles(aujourdhui, retraite);
        EffacerCorrections(jour, aujourdhui);
        bool dejaCouvert = regles.Any(r => r.Jour == jour && r.Fin is null && r.Debut <= aujourdhui);
        if (!dejaCouvert) regles.Add(new RegleLot(jour, aujourdhui, null));
        return avant - CompterTravailles(aujourdhui, retraite);
    }

    /// <summary>« Je travaille de nouveau le <paramref name="jour"/> » à partir d'aujourd'hui. Renvoie la hausse du nombre de jours travaillés.</summary>
    public int TravailleDeNouveau(DayOfWeek jour, DateOnly aujourdhui, DateOnly retraite)
    {
        int avant = CompterTravailles(aujourdhui, retraite);
        EffacerCorrections(jour, aujourdhui);
        regles.RemoveAll(r => r.Jour == jour && r.Debut >= aujourdhui);
        for (int i = 0; i < regles.Count; i++)
        {
            var r = regles[i];
            if (r.Jour == jour && (r.Fin is null || r.Fin.Value >= aujourdhui))
                regles[i] = r with { Fin = aujourdhui.AddDays(-1) };
        }
        return CompterTravailles(aujourdhui, retraite) - avant;
    }

    public int CompterTravailles(DateOnly debutInclus, DateOnly finExclue)
    {
        int n = 0;
        for (var d = debutInclus; d < finExclue; d = d.AddDays(1))
            if (EstTravaille(d)) n++;
        return n;
    }

    /// <summary>Nombre de jours de ce type entre aujourd'hui inclus et la retraite exclue, quel que soit leur statut.</summary>
    public int CompterJoursSemaine(DayOfWeek jour, DateOnly aujourdhui, DateOnly retraite)
    {
        int n = 0;
        for (var d = aujourdhui; d < retraite; d = d.AddDays(1))
            if (d.DayOfWeek == jour) n++;
        return n;
    }

    private TypeJour StatutSansCorrection(DateOnly jour)
    {
        if (EstWeekEnd(jour)) return TypeJour.WeekEnd;
        if (regles.Any(r => r.Couvre(jour))) return TypeJour.NonTravailleLot;
        if (JoursFeries.EstFerie(jour)) return TypeJour.Ferie;
        return TypeJour.Travaille;
    }

    private void EffacerCorrections(DayOfWeek jour, DateOnly aPartirDe)
    {
        foreach (var d in corrections.Keys.Where(d => d.DayOfWeek == jour && d >= aPartirDe).ToList())
            corrections.Remove(d);
    }
}
