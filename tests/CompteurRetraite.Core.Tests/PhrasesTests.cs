using System.Text.RegularExpressions;
using CompteurRetraite.Core.Phrases;

namespace CompteurRetraite.Core.Tests;

public class PhrasesTests
{
    private static readonly IReadOnlyList<Phrase> Banque = BanquePhrases.Charger();
    private static readonly string[] CodesEtape =
        ["jours-1000", "jours-500", "jours-365", "jours-200", "jours-100", "jours-50", "jours-20", "jours-5", "jours-1", "mi-parcours"];

    [Fact]
    public void Banque_contient_100_phrases_valides_et_bien_reparties()
    {
        Assert.Equal(100, Banque.Count);
        Assert.Equal(100, Banque.Select(p => p.Id).Distinct().Count());
        Assert.All(Banque, p => Assert.InRange(p.Texte.Trim().Length, 1, 90));
        Assert.Equal(60, Banque.Count(p => p.Categorie == CategoriePhrase.General));
        Assert.Equal(20, Banque.Count(p => p.Categorie == CategoriePhrase.Saison));
        Assert.Equal(20, Banque.Count(p => p.Categorie == CategoriePhrase.Etape));
        foreach (var code in CodesEtape)
            Assert.Equal(2, Banque.Count(p => p.Etape == code));
        Assert.True(Banque.Count(p => p.Texte.Contains("{prenom}")) >= 15);
    }

    [Fact]
    public void Chaque_jour_de_l_annee_a_au_moins_une_phrase_de_saison()
    {
        var saisons = Banque.Where(p => p.Categorie == CategoriePhrase.Saison).ToList();
        for (var d = new DateOnly(2027, 1, 1); d.Year == 2027; d = d.AddDays(1))
            Assert.True(saisons.Any(p => SelecteurPhrase.EstDeSaison(p, d)), $"aucune saison le {d}");
    }

    [Fact]
    public void Aucune_phrase_ne_tutoie()
    {
        var tutoiement = new Regex(@"\b(tu|te|t|ton|ta|tes|toi)\b", RegexOptions.IgnoreCase);
        Assert.All(Banque, p => Assert.False(tutoiement.IsMatch(p.Texte), p.Texte));
    }

    private static Phrase Choisir(SelecteurPhrase s, DateOnly d, EtatPhrases e, int restants = 5000, int pct = 10, int install = 5000)
        => s.Choisir(d, restants, pct, install, e);

    [Fact]
    public void Meme_date_meme_phrase_et_etat_inchange()
    {
        var s = new SelecteurPhrase(Banque);
        var e = new EtatPhrases();
        var d = new DateOnly(2026, 11, 16);
        var p1 = Choisir(s, d, e);
        int pos = e.Position;
        var p2 = Choisir(s, d, e);
        Assert.Equal(p1, p2);
        Assert.Equal(pos, e.Position);
    }

    [Fact]
    public void Soixante_jours_generaux_sans_repetition()
    {
        var s = new SelecteurPhrase(Banque);
        var e = new EtatPhrases();
        var ids = new List<string>();
        var d = new DateOnly(2026, 11, 16);
        while (ids.Count < 60)
        {
            if (d.DayNumber % 3 != 0) ids.Add(Choisir(s, d, e).Id);
            d = d.AddDays(1);
        }
        Assert.Equal(60, ids.Distinct().Count());
        Assert.All(ids, id => Assert.Equal(CategoriePhrase.General, Banque.Single(p => p.Id == id).Categorie));
    }

    [Fact]
    public void Etape_franchie_le_jour_meme_puis_plus_le_lendemain()
    {
        var s = new SelecteurPhrase(Banque);
        var e = new EtatPhrases();
        var d = new DateOnly(2026, 11, 16);
        Assert.Equal("jours-1000", Choisir(s, d, e, restants: 999, install: 1200).Etape);
        Assert.NotEqual(CategoriePhrase.Etape, Choisir(s, d.AddDays(1), e, restants: 998, install: 1200).Categorie);
    }

    [Fact]
    public void Etape_deja_franchie_a_l_installation_n_est_pas_annoncee()
    {
        var s = new SelecteurPhrase(Banque);
        var p = Choisir(s, new DateOnly(2026, 11, 16), new EtatPhrases(), restants: 999, install: 999);
        Assert.NotEqual(CategoriePhrase.Etape, p.Categorie);
    }

    [Fact]
    public void Mi_parcours_annonce_une_seule_fois()
    {
        var s = new SelecteurPhrase(Banque);
        var e = new EtatPhrases();
        var d = new DateOnly(2026, 11, 16);
        Assert.Equal("mi-parcours", Choisir(s, d, e, restants: 2000, pct: 50, install: 4000).Etape);
        Assert.NotEqual("mi-parcours", Choisir(s, d.AddDays(1), e, restants: 1999, pct: 50, install: 4000).Etape);
    }

    [Fact]
    public void Saison_qui_chevauche_le_nouvel_an()
    {
        var p = new Phrase("x", "x", CategoriePhrase.Saison, "12-15", "01-05", null);
        Assert.True(SelecteurPhrase.EstDeSaison(p, new DateOnly(2027, 1, 2)));
        Assert.True(SelecteurPhrase.EstDeSaison(p, new DateOnly(2026, 12, 20)));
        Assert.False(SelecteurPhrase.EstDeSaison(p, new DateOnly(2027, 1, 6)));
    }

    [Fact]
    public void Un_jour_sur_trois_une_phrase_de_saison()
    {
        var s = new SelecteurPhrase(Banque);
        var d = new DateOnly(2026, 11, 16);
        while (d.DayNumber % 3 != 0) d = d.AddDays(1);
        Assert.Equal(CategoriePhrase.Saison, Choisir(s, d, new EtatPhrases()).Categorie);
    }

    [Fact]
    public void Historique_limite_a_60_entrees()
    {
        var s = new SelecteurPhrase(Banque);
        var e = new EtatPhrases();
        var d = new DateOnly(2026, 1, 1);
        for (int i = 0; i < 100; i++) Choisir(s, d.AddDays(i), e);
        Assert.Equal(60, e.Historique.Count);
    }

    [Fact]
    public void Personnaliser_remplace_le_prenom()
        => Assert.Equal("Bravo Jean-Philippe !", SelecteurPhrase.Personnaliser("Bravo {prenom} !", "Jean-Philippe"));
}
