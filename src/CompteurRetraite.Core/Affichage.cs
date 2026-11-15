using System.Globalization;
using CompteurRetraite.Core.Phrases;

namespace CompteurRetraite.Core;

public enum Mode { AConfigurer, Compte, Retraite }

public sealed record EtatAffichage(Mode Mode, string Phrase, string Chiffre, string Libelle, int Pourcentage, int JoursRestants);

public static class Affichage
{
    private static readonly CultureInfo Fr = new("fr-FR");

    /// <summary>Ce que le bureau doit montrer aujourd'hui. Peut modifier <see cref="Donnees.Phrases"/>.</summary>
    public static EtatAffichage Calculer(Donnees d, SelecteurPhrase selecteur, DateOnly aujourdhui)
    {
        if (d.Retraite is not { } retraite)
            return new(Mode.AConfigurer, "Ouvrez les réglages pour indiquer votre date de retraite.", "", "", 0, 0);

        if (aujourdhui >= retraite)
            return new(Mode.Retraite, $"Bonne retraite, {d.Prenom} !", "", "", 100, 0);

        var c = d.Calendrier();
        int restants = Compteur.JoursRestants(c, aujourdhui, retraite);
        int pct = Compteur.Pourcentage(c, d.Installation, aujourdhui, retraite);
        int restantsInstallation = c.CompterTravailles(d.Installation, retraite);
        var phrase = selecteur.Choisir(aujourdhui, restants, pct, restantsInstallation, d.Phrases);

        string libelle = restants == 1 ? "jour travaillé avant la retraite" : "jours travaillés avant la retraite";
        return new(Mode.Compte, SelecteurPhrase.Personnaliser(phrase.Texte, d.Prenom),
                   restants.ToString("N0", Fr), libelle, pct, restants);
    }
}
