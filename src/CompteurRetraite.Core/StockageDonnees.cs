using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CompteurRetraite.Core;

/// <summary>
/// Lecture et écriture sûres de donnees.json :
/// écriture dans un .tmp vidé sur le disque, puis remplacement en gardant la version précédente (.prec) ;
/// un fichier abîmé est mis de côté (.bak, jamais écrasé) et on repart de la version précédente.
/// </summary>
public sealed class StockageDonnees(string dossier)
{
    private const int Essais = 3;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private string? _dernierContenu;

    public string Dossier { get; } = dossier;
    public string Chemin => Path.Combine(Dossier, "donnees.json");
    private string Precedent => Chemin + ".prec";

    public static string DossierParDefaut() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CompteurRetraite");

    /// <summary>
    /// Charge les données. Lève <see cref="IOException"/> si le fichier reste verrouillé :
    /// dans ce cas rien n'est écrasé, l'appelant doit réessayer plus tard.
    /// </summary>
    public Donnees Charger(DateOnly aujourdhui)
    {
        DateOnly? installationLue = null;

        if (File.Exists(Chemin))
        {
            string texte = LireAvecEssais(Chemin);
            if (Deserialiser(texte) is { } d)
            {
                _dernierContenu = texte;
                if (d.Installation == default) d.Installation = aujourdhui;
                return d;
            }
            installationLue = LireInstallation(texte);
            MettreDeCote();
        }

        if (File.Exists(Precedent) && Deserialiser(LireAvecEssais(Precedent)) is { } recupere)
        {
            Enregistrer(recupere);
            return recupere;
        }

        var neuves = new Donnees { Installation = installationLue ?? aujourdhui };
        Enregistrer(neuves);
        return neuves;
    }

    public void Enregistrer(Donnees d)
    {
        string json = JsonSerializer.Serialize(d, Options);
        if (json == _dernierContenu && File.Exists(Chemin)) return;

        Directory.CreateDirectory(Dossier);
        string tmp = Chemin + ".tmp";
        using (var flux = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            flux.Write(Encoding.UTF8.GetBytes(json));
            flux.Flush(flushToDisk: true);
        }

        if (File.Exists(Chemin)) File.Replace(tmp, Chemin, Precedent);
        else File.Move(tmp, Chemin);
        _dernierContenu = json;
    }

    private static Donnees? Deserialiser(string texte)
    {
        try { return JsonSerializer.Deserialize<Donnees>(texte, Options); }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private static string LireAvecEssais(string chemin)
    {
        for (int essai = 1; ; essai++)
        {
            try { return File.ReadAllText(chemin); }
            catch (IOException) when (essai < Essais && chemin is not null && File.Exists(chemin))
            {
                Thread.Sleep(300);
            }
        }
    }

    /// <summary>Déplace le fichier abîmé vers .bak, ou vers .bak-horodatage si un .bak existe déjà.</summary>
    private void MettreDeCote()
    {
        string cible = Chemin + ".bak";
        if (File.Exists(cible)) cible = $"{Chemin}.bak-{DateTime.Now:yyyyMMdd-HHmmss-fff}";
        File.Move(Chemin, cible);
    }

    private static DateOnly? LireInstallation(string texte)
    {
        try
        {
            var valeur = JsonNode.Parse(texte)?["Installation"]?.GetValue<string>();
            return DateOnly.TryParse(valeur, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
        }
        catch (Exception) { return null; }
    }
}
