namespace CompteurRetraite.Core.Phrases;

/// <summary>Choisit la phrase du jour : étape franchie, sinon saison un jour sur trois, sinon phrase générale sans répétition.</summary>
public sealed class SelecteurPhrase(IReadOnlyList<Phrase> phrases)
{
    private const int TailleHistorique = 60;
    private static readonly (string Code, int Seuil)[] SeuilsJours =
        [("jours-1", 1), ("jours-5", 5), ("jours-20", 20), ("jours-50", 50), ("jours-100", 100),
         ("jours-200", 200), ("jours-365", 365), ("jours-500", 500), ("jours-1000", 1000)];

    private readonly Dictionary<string, Phrase> _parId = phrases.ToDictionary(p => p.Id);

    public Phrase Choisir(DateOnly aujourdhui, int joursRestants, int pourcentage, int joursRestantsInstallation, EtatPhrases etat)
    {
        string cle = aujourdhui.ToString("yyyy-MM-dd");
        if (etat.Historique.TryGetValue(cle, out var id) && _parId.TryGetValue(id, out var deja))
            return deja;

        var phrase = ChoisirEtape(aujourdhui, joursRestants, pourcentage, joursRestantsInstallation, etat)
                     ?? ChoisirSaison(aujourdhui)
                     ?? ChoisirGenerale(aujourdhui, etat);

        etat.Historique[cle] = phrase.Id;
        foreach (var vieille in etat.Historique.Keys.Order(StringComparer.Ordinal).SkipLast(TailleHistorique).ToList())
            etat.Historique.Remove(vieille);
        return phrase;
    }

    public static bool EstDeSaison(Phrase p, DateOnly jour)
    {
        if (p.Debut is null || p.Fin is null) return false;
        int x = jour.Month * 100 + jour.Day;
        int debut = Lire(p.Debut), fin = Lire(p.Fin);
        return debut <= fin ? x >= debut && x <= fin : x >= debut || x <= fin;

        static int Lire(string mmdd) => int.Parse(mmdd[..2]) * 100 + int.Parse(mmdd[3..]);
    }

    public static string Personnaliser(string texte, string prenom) => texte.Replace("{prenom}", prenom);

    private Phrase? ChoisirEtape(DateOnly jour, int restants, int pct, int restantsInstallation, EtatPhrases etat)
    {
        var franchies = SeuilsJours
            .Where(s => restants <= s.Seuil && restantsInstallation > s.Seuil)
            .Select(s => s.Code)
            .ToList();
        if (pct >= 50) franchies.Add("mi-parcours");

        var nouvelles = franchies.Where(c => !etat.EtapesVues.Contains(c)).ToList();
        if (nouvelles.Count == 0) return null;

        etat.EtapesVues.UnionWith(nouvelles);
        var candidates = phrases.Where(p => p.Categorie == CategoriePhrase.Etape && p.Etape == nouvelles[0]).ToList();
        return candidates.Count == 0 ? null : candidates[jour.DayNumber % candidates.Count];
    }

    private Phrase? ChoisirSaison(DateOnly jour)
    {
        if (jour.DayNumber % 3 != 0) return null;
        var saisons = phrases.Where(p => p.Categorie == CategoriePhrase.Saison && EstDeSaison(p, jour)).ToList();
        return saisons.Count == 0 ? null : saisons[jour.DayNumber / 3 % saisons.Count];
    }

    private Phrase ChoisirGenerale(DateOnly jour, EtatPhrases etat)
    {
        for (int essai = 0; essai < 2; essai++)
        {
            while (etat.Position < etat.OrdreGeneral.Count)
            {
                string id = etat.OrdreGeneral[etat.Position++];
                if (_parId.TryGetValue(id, out var p) && p.Categorie == CategoriePhrase.General) return p;
            }
            Melanger(jour, etat);
        }
        throw new InvalidOperationException("Aucune phrase générale disponible.");
    }

    private void Melanger(DateOnly jour, EtatPhrases etat)
    {
        var ids = phrases.Where(p => p.Categorie == CategoriePhrase.General).Select(p => p.Id).ToList();
        var hasard = new Random(jour.DayNumber);
        for (int i = ids.Count - 1; i > 0; i--)
        {
            int j = hasard.Next(i + 1);
            (ids[i], ids[j]) = (ids[j], ids[i]);
        }
        etat.OrdreGeneral = ids;
        etat.Position = 0;
    }
}
