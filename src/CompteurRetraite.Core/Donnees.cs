namespace CompteurRetraite.Core;

public sealed class Donnees
{
    public int Schema { get; set; } = 1;
    public string Prenom { get; set; } = "Jean-Philippe";
    /// <summary>Premier jour de retraite (non compté).</summary>
    public DateOnly? Retraite { get; set; }
    public DateOnly Installation { get; set; }
    public List<RegleLot> Regles { get; set; } = [];
    /// <summary>Corrections manuelles : jour → travaillé ?</summary>
    public Dictionary<DateOnly, bool> Corrections { get; set; } = [];
    public Apparence Apparence { get; set; } = new();
    public bool CompteurMasque { get; set; }

    public CalendrierTravail Calendrier() => new(Regles, Corrections);
}
