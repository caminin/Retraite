using System.Diagnostics;
using System.Reflection;
using CompteurRetraite.Core;
using Microsoft.Win32;

namespace CompteurRetraite.App.Systeme;

/// <summary>Installation dans %LocalAppData%\Programs, lancement à l'ouverture de session (HKCU\…\Run), désinstallation.</summary>
internal static class Installeur
{
    private const string CleRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string NomValeur = "CompteurRetraite";

    public static string DossierProgramme => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CompteurRetraite");

    public static string ExeInstalle => Path.Combine(DossierProgramme, "CompteurRetraite.exe");
    private static string FichierVersion => Path.Combine(DossierProgramme, "version.txt");

    public static Version VersionCourante => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);

    public static bool EstLanceDepuisInstallation(string exe) =>
        string.Equals(Path.GetFullPath(exe), Path.GetFullPath(ExeInstalle), StringComparison.OrdinalIgnoreCase);

    public static void Installer(string exeSource)
    {
        Directory.CreateDirectory(DossierProgramme);
        File.Copy(exeSource, ExeInstalle, overwrite: true);
        RetirerMarqueInternet(ExeInstalle);
        InscrireDemarrage();
    }

    /// <summary>
    /// Un exe téléchargé porte la « marque Internet » (flux Zone.Identifier), copiée avec lui.
    /// On la retire de la copie installée, sinon Windows pourrait redemander confirmation à chaque ouverture de session.
    /// </summary>
    public static void RetirerMarqueInternet(string fichier)
    {
        try { File.Delete(fichier + ":Zone.Identifier"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException) { }
    }

    public static void InscrireDemarrage()
    {
        using var cle = Registry.CurrentUser.CreateSubKey(CleRun);
        cle.SetValue(NomValeur, $"\"{ExeInstalle}\"");
    }

    /// <summary>
    /// Version de l'exe installé. Lue dans version.txt (écrit par l'exe installé à chaque démarrage),
    /// car un exe compilé hors Windows n'a pas de ressource de version.
    /// </summary>
    public static Version? VersionInstallee()
    {
        try
        {
            if (File.Exists(FichierVersion) && Version.TryParse(File.ReadAllText(FichierVersion).Trim(), out var v)) return v;
            if (!File.Exists(ExeInstalle)) return null;
            var info = FileVersionInfo.GetVersionInfo(ExeInstalle);
            return Version.TryParse(info.FileVersion, out var fv) ? fv : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }
    }

    public static void NoterVersionCourante()
    {
        try { File.WriteAllText(FichierVersion, VersionCourante.ToString()); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Journal.Ecrire($"version.txt : {e.Message}"); }
    }

    public static void Lancer(string exe, string arguments = "")
    {
        try { Process.Start(new ProcessStartInfo(exe, arguments) { UseShellExecute = true }); }
        catch (Exception e) { Journal.Ecrire($"Lancement de {exe} impossible : {e.Message}"); }
    }

    /// <summary>Retire le lancement automatique et programme la suppression des dossiers une fois le programme fermé.</summary>
    public static void Desinstaller()
    {
        try
        {
            using var cle = Registry.CurrentUser.OpenSubKey(CleRun, writable: true);
            cle?.DeleteValue(NomValeur, throwOnMissingValue: false);
        }
        catch (Exception e) { Journal.Ecrire($"Clé Run : {e.Message}"); }

        string commande = $"/c ping 127.0.0.1 -n 4 >nul & rmdir /s /q \"{DossierProgramme}\" & rmdir /s /q \"{StockageDonnees.DossierParDefaut()}\"";
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", commande)
            {
                CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetTempPath(),
            });
        }
        catch (Exception e) { Journal.Ecrire($"Suppression programmée impossible : {e.Message}"); }
    }
}
