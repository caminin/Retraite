using System.Net.Http.Headers;
using System.Text.Json;

namespace CompteurRetraite.Core.MiseAJour;

/// <summary>Interroge la dernière release GitHub publique. Ne lève jamais d'exception : renvoie null en cas de doute.</summary>
public sealed class VerificateurMaj(HttpClient http, string? depot, Uri? baseApi = null)
{
    public const string NomAsset = "CompteurRetraite.exe";
    private readonly Uri _baseApi = baseApi ?? new Uri("https://api.github.com/");

    public bool Active => !string.IsNullOrWhiteSpace(depot);

    public async Task<VersionDisponible?> ChercherAsync(Version actuelle, CancellationToken ct)
    {
        if (!Active) return null;
        try
        {
            using var requete = new HttpRequestMessage(HttpMethod.Get, new Uri(_baseApi, $"repos/{depot!.Trim()}/releases/latest"));
            requete.Headers.UserAgent.Add(new ProductInfoHeaderValue("CompteurRetraite", actuelle.ToString()));
            requete.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var reponse = await http.SendAsync(requete, ct).ConfigureAwait(false);
            if (!reponse.IsSuccessStatusCode) return null;

            using var json = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var racine = json.RootElement;
            var version = LireTag(racine.GetProperty("tag_name").GetString() ?? "");
            if (version is null || Normaliser(version) <= Normaliser(actuelle)) return null;

            foreach (var asset in racine.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() != NomAsset) continue;
                string? digest = asset.TryGetProperty("digest", out var dg) && dg.ValueKind == JsonValueKind.String ? dg.GetString() : null;
                return new VersionDisponible(
                    version,
                    new Uri(asset.GetProperty("browser_download_url").GetString()!),
                    asset.GetProperty("size").GetInt64(),
                    digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..] : null);
            }
            return null;
        }
        catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return null;
        }
    }

    public static Version? LireTag(string tag)
    {
        var t = tag.Trim();
        if (t.StartsWith('v') || t.StartsWith('V')) t = t[1..];
        return Version.TryParse(t, out var v) ? v : null;
    }

    /// <summary>1.2 et 1.2.0.0 doivent être égaux : on complète les champs absents par 0.</summary>
    private static Version Normaliser(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
}
