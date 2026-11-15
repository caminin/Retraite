using System.Text.Json;
using System.Text.Json.Serialization;

namespace CompteurRetraite.Core.Phrases;

public static class BanquePhrases
{
    private static readonly Lazy<IReadOnlyList<Phrase>> Phrases = new(Lire);

    public static IReadOnlyList<Phrase> Charger() => Phrases.Value;

    private static IReadOnlyList<Phrase> Lire()
    {
        using var flux = typeof(BanquePhrases).Assembly.GetManifestResourceStream("CompteurRetraite.phrases.json")
            ?? throw new InvalidOperationException("Ressource phrases.json introuvable.");
        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
        return JsonSerializer.Deserialize<List<Phrase>>(flux, options) ?? [];
    }
}
