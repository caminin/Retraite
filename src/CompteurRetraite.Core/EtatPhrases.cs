namespace CompteurRetraite.Core;

/// <summary>Mémoire du choix des phrases : ordre mélangé, historique par date, étapes déjà annoncées.</summary>
public sealed class EtatPhrases
{
    public List<string> OrdreGeneral { get; set; } = [];
    public int Position { get; set; }
    /// <summary>Date « yyyy-MM-dd » → identifiant de la phrase de ce jour.</summary>
    public Dictionary<string, string> Historique { get; set; } = [];
    public HashSet<string> EtapesVues { get; set; } = [];
}
