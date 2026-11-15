using System.Net.Http;
using System.Reflection;
using System.Windows.Threading;
using CompteurRetraite.Core.MiseAJour;

namespace CompteurRetraite.App.Systeme;

/// <summary>
/// Vérifie GitHub 30 s après le démarrage puis toutes les 24 h, télécharge en silence,
/// et installe dès qu'aucune fenêtre de réglage n'est ouverte (sinon au prochain démarrage).
/// </summary>
internal sealed class ServiceMiseAJour : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };
    private readonly VerificateurMaj _verificateur;
    private readonly TelechargeurMaj _telechargeur;
    private readonly DispatcherTimer? _minuterie;
    private readonly Func<bool> _peutRelancer;
    private readonly Action _relancer;
    private readonly CancellationTokenSource _arret = new();
    private bool _enCours;

    public static string FichierNouveau => Installeur.ExeInstalle + ".new";
    private static string FichierPartiel => Installeur.ExeInstalle + ".part";
    private static string FichierTentative => Path.Combine(Installeur.DossierProgramme, "maj-tentee.txt");

    public static string? Depot =>
        Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "DepotGitHub")?.Value;

    public ServiceMiseAJour(Dispatcher ui, Func<bool> peutRelancer, Action relancer)
    {
        _verificateur = new VerificateurMaj(_http, Depot);
        _telechargeur = new TelechargeurMaj(_http);
        _peutRelancer = peutRelancer;
        _relancer = relancer;
        if (!_verificateur.Active) return;

        _minuterie = new DispatcherTimer(DispatcherPriority.Background, ui) { Interval = TimeSpan.FromSeconds(30) };
        _minuterie.Tick += async (_, _) =>
        {
            _minuterie.Interval = TimeSpan.FromHours(24);
            await VerifierAsync();
        };
        _minuterie.Start();
    }

    /// <summary>Au démarrage : si une mise à jour téléchargée attend, l'installe. Renvoie true si l'exe a été remplacé.</summary>
    public static bool AppliquerEnAttente()
    {
        if (!File.Exists(FichierNouveau)) return false;
        bool ok = RemplacementExe.Remplacer(Installeur.ExeInstalle, FichierNouveau);
        if (!ok)
        {
            Journal.Ecrire("Mise à jour en attente impossible à installer ; elle est abandonnée.");
            try { File.Delete(FichierNouveau); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        return ok;
    }

    private async Task VerifierAsync()
    {
        if (_enCours || Environment.ProcessPath is not { } exe || !Installeur.EstLanceDepuisInstallation(exe)) return;
        _enCours = true;
        try
        {
            if (!File.Exists(FichierNouveau))
            {
                var version = await _verificateur.ChercherAsync(Installeur.VersionCourante, _arret.Token);
                if (version is null) return;

                // Garde-fou : si la release annonce une version qui ne se reconnaît pas comme telle, on ne boucle pas.
                string tentative = version.Version.ToString();
                if (File.Exists(FichierTentative) && File.ReadAllText(FichierTentative).Trim() == tentative) return;

                if (!await _telechargeur.TelechargerAsync(version, FichierPartiel, _arret.Token))
                {
                    Journal.Ecrire($"Téléchargement de la version {tentative} échoué.");
                    return;
                }
                File.Move(FichierPartiel, FichierNouveau, overwrite: true);
                File.WriteAllText(FichierTentative, tentative);
            }

            if (_peutRelancer()) _relancer();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Journal.Ecrire($"Mise à jour : {e.Message}");
        }
        catch (OperationCanceledException) { }
        finally { _enCours = false; }
    }

    public void Dispose()
    {
        _arret.Cancel();
        _minuterie?.Stop();
        _http.Dispose();
    }
}
