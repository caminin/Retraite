namespace CompteurRetraite.Core.Tests;

public class CalendrierTravailTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 11, 16); // lundi
    private static readonly DateOnly Retraite = new(2031, 6, 30);
    private static DateOnly D(int m, int j, int a = 2026) => new(a, m, j);

    private static CalendrierTravail Nouveau() => new([], []);

    [Fact]
    public void Statut_weekend_ferie_travaille()
    {
        var c = Nouveau();
        Assert.Equal(TypeJour.WeekEnd, c.Statut(D(11, 21)));
        Assert.Equal(TypeJour.Ferie, c.Statut(D(12, 25)));
        Assert.Equal(TypeJour.Travaille, c.Statut(D(11, 18)));
    }

    [Fact]
    public void Basculer_deux_fois_revient_a_l_etat_initial_sans_correction()
    {
        var corrections = new Dictionary<DateOnly, bool>();
        var c = new CalendrierTravail([], corrections);
        Assert.True(c.Basculer(D(11, 18)));
        Assert.Equal(TypeJour.NonTravailleManuel, c.Statut(D(11, 18)));
        Assert.True(c.Basculer(D(11, 18)));
        Assert.Equal(TypeJour.Travaille, c.Statut(D(11, 18)));
        Assert.Empty(corrections);
    }

    [Fact]
    public void Basculer_un_samedi_ne_fait_rien()
    {
        var c = Nouveau();
        Assert.False(c.Basculer(D(11, 21)));
        Assert.Equal(TypeJour.WeekEnd, c.Statut(D(11, 21)));
    }

    [Fact]
    public void Basculer_un_ferie_le_rend_travaille()
    {
        var c = Nouveau();
        c.Basculer(D(12, 25));
        Assert.Equal(TypeJour.TravailleManuel, c.Statut(D(12, 25)));
        Assert.True(c.EstTravaille(D(12, 25)));
    }

    [Fact]
    public void NeTravaillePlus_marque_les_mercredis_futurs_mais_pas_les_passes()
    {
        var c = Nouveau();
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Equal(TypeJour.NonTravailleLot, c.Statut(D(11, 18)));
        Assert.Equal(TypeJour.Ferie, c.Statut(D(11, 11)));
        Assert.Equal(TypeJour.Travaille, c.Statut(D(11, 4)));
    }

    [Fact]
    public void Correction_manuelle_apres_lot_l_emporte()
    {
        var c = Nouveau();
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        c.Basculer(D(11, 25));
        Assert.Equal(TypeJour.TravailleManuel, c.Statut(D(11, 25)));
    }

    [Fact]
    public void NeTravaillePlus_deux_fois_ne_cree_pas_de_doublon_et_efface_les_corrections()
    {
        var regles = new List<RegleLot>();
        var corrections = new Dictionary<DateOnly, bool>();
        var c = new CalendrierTravail(regles, corrections);
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        c.Basculer(D(11, 25));
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Single(regles);
        Assert.Empty(corrections);
        Assert.Equal(TypeJour.NonTravailleLot, c.Statut(D(11, 25)));
    }

    [Fact]
    public void NeTravaillePlus_puis_TravailleDeNouveau_le_meme_jour_annule_tout()
    {
        var regles = new List<RegleLot>();
        var c = new CalendrierTravail(regles, []);
        int avant = c.CompterTravailles(Aujourdhui, Retraite);
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        c.TravailleDeNouveau(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Empty(regles);
        Assert.Equal(avant, c.CompterTravailles(Aujourdhui, Retraite));
    }

    [Fact]
    public void TravailleDeNouveau_plus_tard_termine_la_regle_la_veille()
    {
        var regles = new List<RegleLot>();
        var c = new CalendrierTravail(regles, []);
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        int n = c.TravailleDeNouveau(DayOfWeek.Wednesday, D(12, 2), Retraite);
        Assert.Equal(D(12, 1), Assert.Single(regles).Fin);
        Assert.Equal(TypeJour.NonTravailleLot, c.Statut(D(11, 25)));
        Assert.Equal(TypeJour.Travaille, c.Statut(D(12, 2)));
        Assert.True(n > 0);
    }

    [Fact]
    public void TravailleDeNouveau_efface_les_corrections_futures_de_ce_jour()
    {
        var corrections = new Dictionary<DateOnly, bool>();
        var c = new CalendrierTravail([], corrections);
        c.Basculer(D(11, 25)); // mercredi en vacances
        c.TravailleDeNouveau(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Empty(corrections);
    }

    [Fact]
    public void CompterTravailles_semaine_normale_et_semaine_avec_ferie()
    {
        var c = Nouveau();
        Assert.Equal(5, c.CompterTravailles(D(11, 16), D(11, 23)));
        Assert.Equal(4, c.CompterTravailles(D(11, 9), D(11, 16)));
    }

    [Fact]
    public void NeTravaillePlus_renvoie_la_baisse_du_compte()
    {
        var c = Nouveau();
        int avant = c.CompterTravailles(Aujourdhui, Retraite);
        int n = c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Equal(avant - n, c.CompterTravailles(Aujourdhui, Retraite));
        Assert.True(n > 200);
    }

    [Fact]
    public void TravailleDeNouveau_renvoie_la_hausse_du_compte()
    {
        var c = Nouveau();
        c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        int avant = c.CompterTravailles(Aujourdhui, Retraite);
        int n = c.TravailleDeNouveau(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Equal(avant + n, c.CompterTravailles(Aujourdhui, Retraite));
    }

    [Fact]
    public void CompterJoursSemaine_compte_tous_les_mercredis_jusqu_a_la_retraite()
    {
        var c = Nouveau();
        // du lundi 16/11 au lundi 30/11 exclu : mercredis 18 et 25
        Assert.Equal(2, c.CompterJoursSemaine(DayOfWeek.Wednesday, Aujourdhui, D(11, 30)));
    }
}

public class ApercuLotTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 11, 16);
    private static readonly DateOnly Retraite = new(2031, 6, 30);

    [Fact]
    public void Copie_n_affecte_pas_l_original()
    {
        var regles = new List<RegleLot>();
        var c = new CalendrierTravail(regles, []);
        c.Copie().NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Empty(regles);
    }

    [Fact]
    public void Apercu_d_une_seconde_application_annonce_zero()
    {
        var c = new CalendrierTravail([], []);
        int annonce = c.Copie().NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite);
        Assert.Equal(annonce, c.NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite));
        Assert.Equal(0, c.Copie().NeTravaillePlus(DayOfWeek.Wednesday, Aujourdhui, Retraite));
    }

    [Theory]
    [InlineData(0, DayOfWeek.Wednesday, "0 mercredi")]
    [InlineData(1, DayOfWeek.Wednesday, "1 mercredi")]
    [InlineData(245, DayOfWeek.Wednesday, "245 mercredis")]
    [InlineData(1200, DayOfWeek.Monday, "1 200 lundis")]
    public void Nombre_de_jours_accorde_en_francais(int n, DayOfWeek jour, string attendu)
        => Assert.Equal(attendu, Libelles.JoursSemaine(n, jour).Replace(' ', ' ').Replace(' ', ' '));
}
