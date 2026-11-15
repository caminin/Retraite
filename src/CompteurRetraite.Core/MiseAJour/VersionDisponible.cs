namespace CompteurRetraite.Core.MiseAJour;

public sealed record VersionDisponible(Version Version, Uri Url, long Taille, string? Sha256);
