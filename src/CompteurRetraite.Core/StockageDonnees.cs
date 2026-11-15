using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CompteurRetraite.Core;

/// <summary>Lecture et écriture atomique de donnees.json.</summary>
public sealed class StockageDonnees(string dossier)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string Dossier { get; } = dossier;
    public string Chemin => Path.Combine(Dossier, "donnees.json");

    public static string DossierParDefaut() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CompteurRetraite");

    public Donnees Charger(DateOnly aujourdhui)
    {
        if (!File.Exists(Chemin))
        {
            var neuves = new Donnees { Installation = aujourdhui };
            Enregistrer(neuves);
            return neuves;
        }

        string texte = File.ReadAllText(Chemin);
        try
        {
            var d = JsonSerializer.Deserialize<Donnees>(texte, Options) ?? throw new JsonException("vide");
            if (d.Installation == default) d.Installation = aujourdhui;
            return d;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException)
        {
            File.Copy(Chemin, Chemin + ".bak", overwrite: true);
            var d = new Donnees { Installation = LireInstallation(texte) ?? aujourdhui };
            Enregistrer(d);
            return d;
        }
    }

    public void Enregistrer(Donnees d)
    {
        Directory.CreateDirectory(Dossier);
        string tmp = Chemin + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(d, Options));
        File.Move(tmp, Chemin, overwrite: true);
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
