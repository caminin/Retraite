namespace CompteurRetraite.Core.Phrases;

public enum CategoriePhrase { General, Saison, Etape }

/// <summary>Une phrase du jour. Pour une saison, <see cref="Debut"/> et <see cref="Fin"/> sont au format « MM-dd », inclus, et peuvent chevaucher le nouvel an.</summary>
public sealed record Phrase(string Id, string Texte, CategoriePhrase Categorie, string? Debut, string? Fin, string? Etape);
