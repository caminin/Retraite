namespace CompteurRetraite.Core;

/// <summary>« Ne travaille pas le <see cref="Jour"/> » du <see cref="Debut"/> au <see cref="Fin"/> inclus (null = sans fin).</summary>
public sealed record RegleLot(DayOfWeek Jour, DateOnly Debut, DateOnly? Fin)
{
    public bool Couvre(DateOnly jour) =>
        jour.DayOfWeek == Jour && jour >= Debut && (Fin is null || jour <= Fin.Value);
}
