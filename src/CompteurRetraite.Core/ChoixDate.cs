namespace CompteurRetraite.Core;

/// <summary>Aide au choix d'une date avec trois listes (jour, mois, année).</summary>
public static class ChoixDate
{
    /// <summary>Compose une date ; un jour trop grand pour le mois est ramené au dernier jour (31 juin → 30 juin).</summary>
    public static DateOnly Composer(int jour, int mois, int annee) =>
        new(annee, mois, Math.Clamp(jour, 1, DateTime.DaysInMonth(annee, mois)));

    /// <summary>Années proposées : de l'année en cours à +15 ans, plus l'année déjà choisie si elle sort de cet intervalle.</summary>
    public static IReadOnlyList<int> Annees(DateOnly aujourdhui, DateOnly? dejaChoisie)
    {
        var annees = Enumerable.Range(aujourdhui.Year, 16).ToList();
        if (dejaChoisie is { } d && !annees.Contains(d.Year)) annees.Add(d.Year);
        annees.Sort();
        return annees;
    }
}
