namespace CompteurRetraite.Core;

public enum Coin { BasDroite, BasGauche, HautDroite, HautGauche }
public enum Taille { Petit, Moyen, Grand }
public enum Theme { Clair, Fonce }

public sealed class Apparence
{
    public Coin Coin { get; set; } = Coin.BasDroite;
    public Taille Taille { get; set; } = Taille.Moyen;
    public Theme Theme { get; set; } = Theme.Clair;
}
