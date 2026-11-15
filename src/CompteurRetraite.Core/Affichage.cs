namespace CompteurRetraite.Core;

public enum Mode { AConfigurer, Compte, Retraite }

/// <summary>Ce que montre le bureau. <see cref="Message"/> n'est rempli que hors décompte (date à saisir, retraite atteinte).</summary>
public sealed record EtatAffichage(Mode Mode, string Message, string Chiffre, string Libelle, int Pourcentage, int JoursRestants);

public static class Affichage
{
    public static EtatAffichage Calculer(Donnees d, DateOnly aujourdhui)
    {
        if (d.Retraite is not { } retraite)
            return new(Mode.AConfigurer, "Ouvrez les réglages pour indiquer votre date de retraite.", "", "", 0, 0);

        if (aujourdhui >= retraite)
            return new(Mode.Retraite, $"Bonne retraite, {d.Prenom} !", "", "", 100, 0);

        var c = d.Calendrier();
        int restants = Compteur.JoursRestants(c, aujourdhui, retraite);
        int pct = Compteur.Pourcentage(c, d.Installation, aujourdhui, retraite);
        string libelle = restants == 1 ? "jour travaillé avant la retraite" : "jours travaillés avant la retraite";
        return new(Mode.Compte, "", Libelles.Nombre(restants), libelle, pct, restants);
    }
}
