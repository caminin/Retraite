using System.Net;
using System.Security.Cryptography;
using System.Text;
using CompteurRetraite.Core.MiseAJour;

namespace CompteurRetraite.Core.Tests;

/// <summary>Faux serveur HTTP : renvoie une réponse par URL et compte les appels.</summary>
internal sealed class FauxServeur(Func<HttpRequestMessage, HttpResponseMessage> repondre) : HttpMessageHandler
{
    public int Appels { get; private set; }
    public List<HttpRequestMessage> Requetes { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
    {
        Appels++;
        Requetes.Add(r);
        return Task.FromResult(repondre(r));
    }

    public static HttpResponseMessage Texte(string corps, HttpStatusCode code = HttpStatusCode.OK)
        => new(code) { Content = new StringContent(corps) };
}

public sealed class MiseAJourTests : IDisposable
{
    private readonly string _dossier = Directory.CreateTempSubdirectory("cr-maj-").FullName;
    public void Dispose() => Directory.Delete(_dossier, recursive: true);

    private static readonly Version Actuelle = new(0, 1, 0);

    private static string Release(string tag, string nomAsset = "CompteurRetraite.exe", long taille = 4, string digest = "sha256:abcdef") => $$"""
        { "tag_name": "{{tag}}",
          "assets": [
            { "name": "autre.zip", "size": 1, "browser_download_url": "https://exemple/autre.zip", "digest": null },
            { "name": "{{nomAsset}}", "size": {{taille}}, "browser_download_url": "https://exemple/CompteurRetraite.exe", "digest": "{{digest}}" }
          ] }
        """;

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("v2.0", "2.0")]
    public void LireTag_accepte_les_formats_courants(string tag, string attendu)
        => Assert.Equal(Version.Parse(attendu), VerificateurMaj.LireTag(tag));

    [Fact]
    public void LireTag_refuse_le_reste() => Assert.Null(VerificateurMaj.LireTag("abc"));

    [Fact]
    public async Task Sans_depot_rien_n_est_appele()
    {
        var serveur = new FauxServeur(_ => throw new InvalidOperationException());
        var v = new VerificateurMaj(new HttpClient(serveur), "");
        Assert.False(v.Active);
        Assert.Null(await v.ChercherAsync(Actuelle, CancellationToken.None));
        Assert.Equal(0, serveur.Appels);
    }

    [Fact]
    public async Task Version_plus_recente_trouvee()
    {
        var serveur = new FauxServeur(_ => FauxServeur.Texte(Release("v0.2.0")));
        var v = new VerificateurMaj(new HttpClient(serveur), "moi/compteur");
        var trouvee = await v.ChercherAsync(Actuelle, CancellationToken.None);
        Assert.NotNull(trouvee);
        Assert.Equal(new Version(0, 2, 0), trouvee.Version);
        Assert.Equal(new Uri("https://exemple/CompteurRetraite.exe"), trouvee.Url);
        Assert.Equal(4, trouvee.Taille);
        Assert.Equal("abcdef", trouvee.Sha256);
        var requete = Assert.Single(serveur.Requetes);
        Assert.Equal("https://api.github.com/repos/moi/compteur/releases/latest", requete.RequestUri!.ToString());
        Assert.Contains("CompteurRetraite", requete.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Meme_version_renvoie_null()
    {
        var v = new VerificateurMaj(new HttpClient(new FauxServeur(_ => FauxServeur.Texte(Release("v0.1.0")))), "moi/compteur");
        Assert.Null(await v.ChercherAsync(Actuelle, CancellationToken.None));
    }

    [Fact]
    public async Task Sans_asset_exe_renvoie_null()
    {
        var v = new VerificateurMaj(new HttpClient(new FauxServeur(_ => FauxServeur.Texte(Release("v0.2.0", "setup.msi")))), "moi/compteur");
        Assert.Null(await v.ChercherAsync(Actuelle, CancellationToken.None));
    }

    [Fact]
    public async Task Erreur_404_renvoie_null()
    {
        var v = new VerificateurMaj(new HttpClient(new FauxServeur(_ => FauxServeur.Texte("{}", HttpStatusCode.NotFound))), "moi/compteur");
        Assert.Null(await v.ChercherAsync(Actuelle, CancellationToken.None));
    }

    [Fact]
    public async Task Reponse_illisible_renvoie_null()
    {
        var v = new VerificateurMaj(new HttpClient(new FauxServeur(_ => FauxServeur.Texte("<html>"))), "moi/compteur");
        Assert.Null(await v.ChercherAsync(Actuelle, CancellationToken.None));
    }

    [Fact]
    public async Task Panne_reseau_renvoie_null()
    {
        var v = new VerificateurMaj(new HttpClient(new FauxServeur(_ => throw new HttpRequestException("pas de réseau"))), "moi/compteur");
        Assert.Null(await v.ChercherAsync(Actuelle, CancellationToken.None));
    }

    private static readonly byte[] Contenu = Encoding.UTF8.GetBytes("NOUVEL EXE");
    private static string Sha(byte[] b) => Convert.ToHexStringLower(SHA256.HashData(b));

    private TelechargeurMaj Telechargeur() => new(new HttpClient(new FauxServeur(_ =>
        new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Contenu) })));

    [Fact]
    public async Task Telechargement_verifie_et_ecrit_le_fichier()
    {
        string dest = Path.Combine(_dossier, "nouveau.exe");
        var v = new VersionDisponible(new Version(0, 2, 0), new Uri("https://exemple/x"), Contenu.Length, Sha(Contenu));
        Assert.True(await Telechargeur().TelechargerAsync(v, dest, CancellationToken.None));
        Assert.Equal(Contenu, File.ReadAllBytes(dest));
    }

    [Fact]
    public async Task Mauvaise_empreinte_supprime_le_fichier()
    {
        string dest = Path.Combine(_dossier, "nouveau.exe");
        var v = new VersionDisponible(new Version(0, 2, 0), new Uri("https://exemple/x"), Contenu.Length, new string('0', 64));
        Assert.False(await Telechargeur().TelechargerAsync(v, dest, CancellationToken.None));
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public async Task Mauvaise_taille_supprime_le_fichier()
    {
        string dest = Path.Combine(_dossier, "nouveau.exe");
        var v = new VersionDisponible(new Version(0, 2, 0), new Uri("https://exemple/x"), 999, null);
        Assert.False(await Telechargeur().TelechargerAsync(v, dest, CancellationToken.None));
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void Remplacer_met_le_nouveau_en_place_et_garde_l_ancien_en_old()
    {
        string actuel = Path.Combine(_dossier, "app.exe"), nouveau = actuel + ".new";
        File.WriteAllText(actuel, "ANCIEN");
        File.WriteAllText(nouveau, "NOUVEAU");
        Assert.True(RemplacementExe.Remplacer(actuel, nouveau));
        Assert.Equal("NOUVEAU", File.ReadAllText(actuel));
        Assert.Equal("ANCIEN", File.ReadAllText(actuel + ".old"));
        Assert.False(File.Exists(nouveau));
    }

    [Fact]
    public void Remplacer_sans_nouveau_laisse_l_actuel_intact()
    {
        string actuel = Path.Combine(_dossier, "app.exe");
        File.WriteAllText(actuel, "ANCIEN");
        Assert.False(RemplacementExe.Remplacer(actuel, actuel + ".new"));
        Assert.Equal("ANCIEN", File.ReadAllText(actuel));
    }

    [Fact]
    public void Remplacer_ecrase_un_ancien_old()
    {
        string actuel = Path.Combine(_dossier, "app.exe");
        File.WriteAllText(actuel, "V1");
        File.WriteAllText(actuel + ".old", "V0");
        File.WriteAllText(actuel + ".new", "V2");
        Assert.True(RemplacementExe.Remplacer(actuel, actuel + ".new"));
        Assert.Equal("V1", File.ReadAllText(actuel + ".old"));
    }

    [Fact]
    public void Nettoyer_supprime_old_sans_erreur_meme_absent()
    {
        string actuel = Path.Combine(_dossier, "app.exe");
        RemplacementExe.Nettoyer(actuel);
        File.WriteAllText(actuel + ".old", "x");
        RemplacementExe.Nettoyer(actuel);
        Assert.False(File.Exists(actuel + ".old"));
    }
}
