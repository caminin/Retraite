using System.IO.Pipes;

namespace CompteurRetraite.App.Systeme;

/// <summary>Une seule instance à la fois. Les autres lancements envoient une commande (REGLAGES, QUITTER) par un canal nommé.</summary>
internal sealed class InstanceUnique : IDisposable
{
    private const string NomMutex = @"Local\CompteurRetraite";
    private const string NomCanal = "CompteurRetraite";

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _arret = new();

    public bool EstPremiere { get; }
    public event Action<string>? CommandeRecue;

    public InstanceUnique()
    {
        _mutex = new Mutex(true, NomMutex, out bool cree);
        EstPremiere = cree;
        if (cree) _ = Task.Run(EcouterAsync);
    }

    public static bool InstanceEnCours()
    {
        if (!Mutex.TryOpenExisting(NomMutex, out var m)) return false;
        m.Dispose();
        return true;
    }

    public static bool AttendreFin(TimeSpan delai)
    {
        var limite = DateTime.UtcNow + delai;
        while (InstanceEnCours())
        {
            if (DateTime.UtcNow > limite) return false;
            Thread.Sleep(200);
        }
        return true;
    }

    public static bool Envoyer(string commande)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", NomCanal, PipeDirection.Out);
            client.Connect(3000);
            using var ecrivain = new StreamWriter(client) { AutoFlush = true };
            ecrivain.WriteLine(commande);
            return true;
        }
        catch (Exception e)
        {
            Journal.Ecrire($"Envoi de {commande} impossible : {e.Message}");
            return false;
        }
    }

    private async Task EcouterAsync()
    {
        while (!_arret.IsCancellationRequested)
        {
            try
            {
                await using var serveur = new NamedPipeServerStream(NomCanal, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await serveur.WaitForConnectionAsync(_arret.Token);
                using var lecteur = new StreamReader(serveur);
                var ligne = await lecteur.ReadLineAsync(_arret.Token);
                if (!string.IsNullOrWhiteSpace(ligne)) CommandeRecue?.Invoke(ligne.Trim());
            }
            catch (OperationCanceledException) { return; }
            catch (Exception e)
            {
                Journal.Ecrire($"Canal : {e.Message}");
                await Task.Delay(1000);
            }
        }
    }

    public void Dispose()
    {
        _arret.Cancel();
        if (EstPremiere)
        {
            try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
        }
        _mutex.Dispose();
    }
}
