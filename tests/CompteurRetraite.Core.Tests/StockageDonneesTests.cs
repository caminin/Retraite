namespace CompteurRetraite.Core.Tests;

public sealed class StockageDonneesTests : IDisposable
{
    private readonly string _dossier = Directory.CreateTempSubdirectory("cr-test-").FullName;
    private static readonly DateOnly Aujourdhui = new(2026, 11, 16);

    public void Dispose() => Directory.Delete(_dossier, recursive: true);

    private string Fichier => Path.Combine(_dossier, "donnees.json");

    [Fact]
    public void Aller_retour_complet()
    {
        var s = new StockageDonnees(_dossier);
        var d = new Donnees
        {
            Prenom = "Jean-Philippe",
            Retraite = new DateOnly(2031, 6, 30),
            Installation = new DateOnly(2026, 11, 15),
            CompteurMasque = true,
        };
        d.Regles.Add(new RegleLot(DayOfWeek.Wednesday, Aujourdhui, null));
        d.Corrections[new DateOnly(2026, 12, 24)] = false;
        d.Apparence.Coin = Coin.HautGauche;
        d.Apparence.Taille = Taille.Grand;
        d.Apparence.Theme = Theme.Fonce;
        d.Phrases.OrdreGeneral.AddRange(["g01", "g02"]);
        d.Phrases.Position = 1;
        d.Phrases.Historique["2026-11-16"] = "g01";
        d.Phrases.EtapesVues.Add("jours-1000");

        s.Enregistrer(d);
        var lu = s.Charger(Aujourdhui);

        Assert.Equal(d.Retraite, lu.Retraite);
        Assert.Equal(d.Installation, lu.Installation);
        Assert.True(lu.CompteurMasque);
        Assert.Equal(d.Regles, lu.Regles);
        Assert.False(lu.Corrections[new DateOnly(2026, 12, 24)]);
        Assert.Equal(Coin.HautGauche, lu.Apparence.Coin);
        Assert.Equal(Taille.Grand, lu.Apparence.Taille);
        Assert.Equal(Theme.Fonce, lu.Apparence.Theme);
        Assert.Equal(["g01", "g02"], lu.Phrases.OrdreGeneral);
        Assert.Equal(1, lu.Phrases.Position);
        Assert.Equal("g01", lu.Phrases.Historique["2026-11-16"]);
        Assert.Contains("jours-1000", lu.Phrases.EtapesVues);
    }

    [Fact]
    public void Fichier_absent_cree_les_valeurs_par_defaut_avec_installation_aujourdhui()
    {
        var lu = new StockageDonnees(_dossier).Charger(Aujourdhui);
        Assert.Equal(Aujourdhui, lu.Installation);
        Assert.Equal("Jean-Philippe", lu.Prenom);
        Assert.Null(lu.Retraite);
        Assert.True(File.Exists(Fichier));
    }

    [Fact]
    public void Fichier_corrompu_est_mis_de_cote_et_on_repart_des_valeurs_par_defaut()
    {
        File.WriteAllText(Fichier, "{ pas du json");
        var lu = new StockageDonnees(_dossier).Charger(Aujourdhui);
        Assert.True(File.Exists(Fichier + ".bak"));
        Assert.Equal(Aujourdhui, lu.Installation);
        Assert.Empty(lu.Regles);
    }

    [Fact]
    public void Fichier_corrompu_garde_la_date_d_installation_si_lisible()
    {
        File.WriteAllText(Fichier, """{"Installation":"2025-01-02","Regles":42}""");
        var lu = new StockageDonnees(_dossier).Charger(Aujourdhui);
        Assert.Equal(new DateOnly(2025, 1, 2), lu.Installation);
    }

    [Fact]
    public void Un_tmp_reste_d_une_ecriture_interrompue_ne_gene_pas()
    {
        var s = new StockageDonnees(_dossier);
        s.Enregistrer(new Donnees { Installation = new DateOnly(2026, 1, 5) });
        File.WriteAllText(Fichier + ".tmp", "{ moitié");
        Assert.Equal(new DateOnly(2026, 1, 5), s.Charger(Aujourdhui).Installation);
        s.Enregistrer(new Donnees { Installation = new DateOnly(2026, 1, 6) });
        Assert.Equal(new DateOnly(2026, 1, 6), s.Charger(Aujourdhui).Installation);
    }

    [Fact]
    public void Calendrier_partage_les_collections_des_donnees()
    {
        var d = new Donnees();
        d.Calendrier().Basculer(new DateOnly(2026, 11, 18));
        Assert.Single(d.Corrections);
    }
}

public sealed class StockageRobustesseTests : IDisposable
{
    private readonly string _dossier = Directory.CreateTempSubdirectory("cr-rob-").FullName;
    private static readonly DateOnly Aujourdhui = new(2026, 11, 16);
    public void Dispose() => Directory.Delete(_dossier, recursive: true);
    private string Fichier => Path.Combine(_dossier, "donnees.json");

    [Fact]
    public void Enregistrer_garde_la_version_precedente()
    {
        var s = new StockageDonnees(_dossier);
        s.Enregistrer(new Donnees { Installation = Aujourdhui, Prenom = "Un" });
        s.Enregistrer(new Donnees { Installation = Aujourdhui, Prenom = "Deux" });
        Assert.Contains("Un", File.ReadAllText(Fichier + ".prec"));
    }

    [Fact]
    public void Fichier_illisible_recupere_la_version_precedente_avec_les_vacances()
    {
        var s = new StockageDonnees(_dossier);
        var d = new Donnees { Installation = new DateOnly(2025, 3, 1), Retraite = new DateOnly(2031, 6, 30) };
        d.Corrections[new DateOnly(2026, 12, 24)] = false;
        s.Enregistrer(d);
        d.Prenom = "Autre";
        s.Enregistrer(d);
        File.WriteAllBytes(Fichier, new byte[64]); // coupure de courant : fichier rempli de zéros

        var lu = new StockageDonnees(_dossier).Charger(Aujourdhui);
        Assert.Equal(new DateOnly(2031, 6, 30), lu.Retraite);
        Assert.Equal(new DateOnly(2025, 3, 1), lu.Installation);
        Assert.Single(lu.Corrections);
    }

    [Fact]
    public void Un_bak_existant_n_est_jamais_ecrase()
    {
        File.WriteAllText(Fichier + ".bak", "ancien");
        File.WriteAllText(Fichier, "{ abîmé");
        new StockageDonnees(_dossier).Charger(Aujourdhui);
        Assert.Equal("ancien", File.ReadAllText(Fichier + ".bak"));
        Assert.Equal(2, Directory.GetFiles(_dossier, "donnees.json.bak*").Length);
    }

    [Fact]
    public void Fichier_verrouille_leve_une_erreur_sans_rien_ecraser()
    {
        var s = new StockageDonnees(_dossier);
        s.Enregistrer(new Donnees { Installation = new DateOnly(2025, 3, 1), Retraite = new DateOnly(2031, 6, 30) });
        string avant = File.ReadAllText(Fichier);
        using (new FileStream(Fichier, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.ThrowsAny<IOException>(() => new StockageDonnees(_dossier).Charger(Aujourdhui));
        Assert.Equal(avant, File.ReadAllText(Fichier));
    }

    [Fact]
    public void Enregistrer_n_ecrit_pas_si_rien_n_a_change()
    {
        var s = new StockageDonnees(_dossier);
        var d = new Donnees { Installation = Aujourdhui };
        s.Enregistrer(d);
        var ancienne = new DateTime(2020, 1, 1);
        File.SetLastWriteTime(Fichier, ancienne);
        s.Enregistrer(d);
        Assert.Equal(ancienne, File.GetLastWriteTime(Fichier));
    }
}
