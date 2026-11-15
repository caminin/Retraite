namespace CompteurRetraite.Core.Tests;

public class CompteurTests
{
    private static DateOnly D(int m, int j, int a = 2026) => new(a, m, j);
    private static CalendrierTravail Nouveau() => new([], []);

    [Fact]
    public void JoursRestants_compte_aujourdhui_et_exclut_la_retraite()
    {
        var c = Nouveau();
        Assert.Equal(5, Compteur.JoursRestants(c, D(11, 16), D(11, 21)));
        Assert.Equal(1, Compteur.JoursRestants(c, D(11, 20), D(11, 21)));
    }

    [Fact]
    public void JoursRestants_vaut_zero_le_jour_de_la_retraite_et_apres()
    {
        var c = Nouveau();
        Assert.Equal(0, Compteur.JoursRestants(c, D(11, 21), D(11, 21)));
        Assert.Equal(0, Compteur.JoursRestants(c, D(11, 22), D(11, 21)));
    }

    [Fact]
    public void Pourcentage_vaut_zero_le_jour_de_l_installation()
        => Assert.Equal(0, Compteur.Pourcentage(Nouveau(), D(11, 16), D(11, 16), new DateOnly(2031, 6, 30)));

    [Fact]
    public void Pourcentage_a_mi_chemin()
        => Assert.Equal(50, Compteur.Pourcentage(Nouveau(), D(11, 16), D(11, 23), D(11, 30)));

    [Fact]
    public void Pourcentage_n_avance_pas_pendant_les_vacances()
    {
        var c = Nouveau();
        for (int j = 16; j <= 20; j++) c.Basculer(D(11, j));
        Assert.Equal(0, Compteur.Pourcentage(c, D(11, 16), D(11, 23), D(11, 30)));
    }

    [Fact]
    public void Pourcentage_vaut_100_si_la_retraite_precede_l_installation()
        => Assert.Equal(100, Compteur.Pourcentage(Nouveau(), D(11, 16), D(11, 16), D(11, 1)));

    [Fact]
    public void Pourcentage_est_borne_a_100_apres_la_retraite()
        => Assert.Equal(100, Compteur.Pourcentage(Nouveau(), D(11, 16), D(12, 15), D(11, 30)));
}
