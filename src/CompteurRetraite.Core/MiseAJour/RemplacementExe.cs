namespace CompteurRetraite.Core.MiseAJour;

/// <summary>Remplace un exe (éventuellement en cours d'exécution : Windows autorise à le renommer).</summary>
public static class RemplacementExe
{
    public static bool Remplacer(string exeActuel, string exeNouveau)
    {
        if (!File.Exists(exeNouveau)) return false;
        string ancien = exeActuel + ".old";
        try
        {
            if (File.Exists(exeActuel)) File.Move(exeActuel, ancien, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        try
        {
            File.Move(exeNouveau, exeActuel);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try { if (File.Exists(ancien)) File.Move(ancien, exeActuel, overwrite: true); }
            catch (Exception r) when (r is IOException or UnauthorizedAccessException) { }
            return false;
        }
    }

    public static void Nettoyer(string exeActuel)
    {
        try { File.Delete(exeActuel + ".old"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
