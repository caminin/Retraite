namespace CompteurRetraite.Core.Tests;

public class AffichageTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 11, 16);

    [Fact]
    public void Sans_date_de_retraite_invite_a_regler()
    {
        var e = Affichage.Calculer(new Donnees { Installation = Aujourdhui }, Aujourdhui);
        Assert.Equal(Mode.AConfigurer, e.Mode);
        Assert.Equal("Ouvrez les réglages pour indiquer votre date de retraite.", e.Message);
        Assert.Equal("", e.Chiffre);
    }

    [Fact]
    public void Le_jour_de_la_retraite_affiche_bonne_retraite()
    {
        var d = new Donnees { Installation = new DateOnly(2026, 1, 1), Retraite = Aujourdhui, Prenom = "Jean-Philippe" };
        var e = Affichage.Calculer(d, Aujourdhui);
        Assert.Equal(Mode.Retraite, e.Mode);
        Assert.Equal("Bonne retraite, Jean-Philippe !", e.Message);
        Assert.Equal(100, e.Pourcentage);
    }

    [Fact]
    public void En_cours_affiche_le_chiffre_formate_et_le_libelle()
    {
        var d = new Donnees { Installation = Aujourdhui, Retraite = new DateOnly(2031, 6, 30) };
        var e = Affichage.Calculer(d, Aujourdhui);
        Assert.Equal(Mode.Compte, e.Mode);
        int restants = d.Calendrier().CompterTravailles(Aujourdhui, d.Retraite.Value);
        Assert.Equal(restants.ToString("N0", new System.Globalization.CultureInfo("fr-FR")), e.Chiffre);
        Assert.Matches(@"^\d[\d\u202F\u00A0]\d{3}$", e.Chiffre);
        Assert.Equal("jours travaillés avant la retraite", e.Libelle);
        Assert.Equal("", e.Message); // plus de phrase du jour : seul le chiffre s'affiche
    }

    [Fact]
    public void Singulier_pour_un_seul_jour()
    {
        var d = new Donnees { Installation = Aujourdhui, Retraite = Aujourdhui.AddDays(1) };
        var e = Affichage.Calculer(d, Aujourdhui);
        Assert.Equal("1", e.Chiffre);
        Assert.Equal("jour travaillé avant la retraite", e.Libelle);
    }

    [Fact]
    public void Retraite_avant_installation_ne_plante_pas()
    {
        var d = new Donnees { Installation = Aujourdhui, Retraite = Aujourdhui.AddDays(-30) };
        var e = Affichage.Calculer(d, Aujourdhui);
        Assert.Equal(Mode.Retraite, e.Mode);
    }

    [Fact]
    public void Retraite_future_mais_installation_apres_aujourdhui_ne_plante_pas()
    {
        var d = new Donnees { Installation = Aujourdhui.AddDays(10), Retraite = Aujourdhui.AddDays(100) };
        var e = Affichage.Calculer(d, Aujourdhui);
        Assert.Equal(Mode.Compte, e.Mode);
        Assert.InRange(e.Pourcentage, 0, 100);
    }
}

public class HorlogeTests
{
    [Fact]
    public void Juste_avant_minuit()
        => Assert.Equal(TimeSpan.FromSeconds(62), Horloge.JusquAuProchainMinuit(new DateTime(2026, 11, 16, 23, 59, 0)));

    [Fact]
    public void Pile_a_minuit_on_attend_le_minuit_suivant()
        => Assert.Equal(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(2), Horloge.JusquAuProchainMinuit(new DateTime(2026, 11, 16, 0, 0, 0)));
}

public class DecisionLancementTests
{
    private const string Installe = @"C:\Users\jp\AppData\Local\Programs\CompteurRetraite\CompteurRetraite.exe";
    private const string Telecharge = @"C:\Users\jp\Downloads\CompteurRetraite.exe";
    private static readonly Version V1 = new(0, 1, 0), V2 = new(0, 2, 0);

    [Theory]
    [InlineData(Installe, true, true, ActionLancement.SignalerInstanceEnCours)]
    [InlineData(Installe, true, false, ActionLancement.Demarrer)]
    [InlineData(@"c:\users\JP\appdata\local\programs\compteurretraite\COMPTEURRETRAITE.EXE", true, false, ActionLancement.Demarrer)]
    public void Lance_depuis_le_dossier_installe(string lance, bool existe, bool enCours, ActionLancement attendu)
        => Assert.Equal(attendu, DecisionLancement.Decider(lance, Installe, existe, V1, V1, enCours));

    [Fact]
    public void Premiere_installation()
        => Assert.Equal(ActionLancement.Installer, DecisionLancement.Decider(Telecharge, Installe, false, V1, null, false));

    [Fact]
    public void Version_plus_recente_remplace()
        => Assert.Equal(ActionLancement.RemplacerEtRelancer, DecisionLancement.Decider(Telecharge, Installe, true, V2, V1, true));

    [Fact]
    public void Version_installee_inconnue_remplace()
        => Assert.Equal(ActionLancement.RemplacerEtRelancer, DecisionLancement.Decider(Telecharge, Installe, true, V1, null, false));

    [Fact]
    public void Meme_version_ouvre_les_reglages_si_en_cours()
        => Assert.Equal(ActionLancement.SignalerInstanceEnCours, DecisionLancement.Decider(Telecharge, Installe, true, V1, V1, true));

    [Fact]
    public void Version_plus_ancienne_demarre_l_installee_si_rien_ne_tourne()
        => Assert.Equal(ActionLancement.Demarrer, DecisionLancement.Decider(Telecharge, Installe, true, V1, V2, false));
}
