namespace CompteurRetraite.Core;

public static class Compteur
{
    /// <summary>Jours travaillés d'aujourd'hui (inclus) à la retraite (exclue).</summary>
    public static int JoursRestants(CalendrierTravail c, DateOnly aujourdhui, DateOnly retraite)
        => aujourdhui >= retraite ? 0 : c.CompterTravailles(aujourdhui, retraite);

    /// <summary>Part des jours travaillés depuis l'installation, de 0 à 100.</summary>
    public static int Pourcentage(CalendrierTravail c, DateOnly installation, DateOnly aujourdhui, DateOnly retraite)
    {
        int total = c.CompterTravailles(installation, retraite);
        if (total <= 0) return 100;
        var fin = aujourdhui < retraite ? aujourdhui : retraite;
        int faits = c.CompterTravailles(installation, fin);
        int pct = (int)Math.Round(100.0 * faits / total, MidpointRounding.AwayFromZero);
        return Math.Clamp(pct, 0, 100);
    }
}
