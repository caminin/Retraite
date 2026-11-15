using CompteurRetraite.Core;

namespace CompteurRetraite.App.Systeme;

/// <summary>Petit journal d'erreurs dans le dossier des données, limité à 100 Ko. Ne lève jamais d'exception.</summary>
internal static class Journal
{
    private const long TailleMax = 100 * 1024;
    private static readonly object Verrou = new();

    public static string Chemin => Path.Combine(StockageDonnees.DossierParDefaut(), "journal.txt");

    public static void Ecrire(string message)
    {
        try
        {
            lock (Verrou)
            {
                Directory.CreateDirectory(StockageDonnees.DossierParDefaut());
                if (File.Exists(Chemin) && new FileInfo(Chemin).Length > TailleMax)
                    File.Move(Chemin, Chemin + ".1", overwrite: true);
                File.AppendAllText(Chemin, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
        }
        catch (Exception) { /* le journal ne doit jamais gêner */ }
    }
}
