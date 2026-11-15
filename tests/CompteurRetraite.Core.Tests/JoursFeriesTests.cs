namespace CompteurRetraite.Core.Tests;

public class JoursFeriesTests
{
    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2030, 4, 21)]
    [InlineData(2031, 4, 13)]
    public void Paques_donne_la_bonne_date(int annee, int mois, int jour)
        => Assert.Equal(new DateOnly(annee, mois, jour), JoursFeries.Paques(annee));

    [Fact]
    public void PourAnnee_2026_contient_les_11_feries()
    {
        var attendus = new[]
        {
            new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 6), new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 8), new DateOnly(2026, 5, 14), new DateOnly(2026, 5, 25),
            new DateOnly(2026, 7, 14), new DateOnly(2026, 8, 15), new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 11), new DateOnly(2026, 12, 25),
        };
        var feries = JoursFeries.PourAnnee(2026);
        Assert.Equal(11, feries.Count);
        Assert.All(attendus, d => Assert.Contains(d, feries));
    }

    [Fact]
    public void EstFerie_noel_oui_veille_non()
    {
        Assert.True(JoursFeries.EstFerie(new DateOnly(2026, 12, 25)));
        Assert.False(JoursFeries.EstFerie(new DateOnly(2026, 12, 24)));
    }
}
