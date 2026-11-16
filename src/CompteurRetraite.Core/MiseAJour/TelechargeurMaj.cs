using System.Security.Cryptography;

namespace CompteurRetraite.Core.MiseAJour;

public sealed class TelechargeurMaj(HttpClient http)
{
    /// <summary>Télécharge vers <paramref name="destination"/>, vérifie taille et SHA-256. En cas d'échec, supprime le fichier et renvoie false.</summary>
    public async Task<bool> TelechargerAsync(VersionDisponible v, string destination, CancellationToken ct)
    {
        try
        {
            using (var reponse = await http.GetAsync(v.Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                reponse.EnsureSuccessStatusCode();
                await using var source = await reponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using var cible = File.Create(destination);
                await source.CopyToAsync(cible, ct).ConfigureAwait(false);
            }

            if (new FileInfo(destination).Length != v.Taille) return Echec(destination);
            if (v.Sha256 is not null)
            {
                string empreinte;
                // Le fichier doit être fermé avant une éventuelle suppression : Windows refuse de supprimer un fichier ouvert.
                await using (var lecture = File.OpenRead(destination))
                    empreinte = Convert.ToHexStringLower(await SHA256.HashDataAsync(lecture, ct).ConfigureAwait(false));
                if (!string.Equals(empreinte, v.Sha256, StringComparison.OrdinalIgnoreCase)) return Echec(destination);
            }
            return true;
        }
        catch (Exception)
        {
            return Echec(destination);
        }
    }

    private static bool Echec(string fichier)
    {
        try { File.Delete(fichier); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return false;
    }
}
