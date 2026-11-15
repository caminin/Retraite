# Préparation de la VM de test : accès SSH par clé (pour piloter les tests depuis Linux),
# pas de mise en veille ni d'écran de verrouillage.
$ErrorActionPreference = 'Continue'
Write-Output "Début : $(Get-Date)"

# Ni veille ni extinction d'écran.
powercfg /change monitor-timeout-ac 0
powercfg /change standby-timeout-ac 0
powercfg /change hibernate-timeout-ac 0

# Serveur OpenSSH (fonctionnalité Windows à la demande).
$ok = $false
for ($i = 1; $i -le 5 -and -not $ok; $i++) {
    try {
        Add-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0' -ErrorAction Stop | Out-Null
        $ok = $true
    } catch {
        Write-Output "Essai $i : $($_.Exception.Message)"
        Start-Sleep -Seconds 30
    }
}
Set-Service sshd -StartupType Automatic
Start-Service sshd
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -ErrorAction SilentlyContinue | Out-Null

# Connexion par clé uniquement (le compte est administrateur).
$cles = 'C:\ProgramData\ssh\administrators_authorized_keys'
Copy-Item 'C:\OEM\cle_test.pub' $cles -Force
icacls $cles /inheritance:r /grant '*S-1-5-32-544:F' /grant '*S-1-5-18:F' | Out-Null

# PowerShell comme shell SSH.
New-Item -Path 'HKLM:\SOFTWARE\OpenSSH' -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\OpenSSH' -Name DefaultShell -Value 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -PropertyType String -Force | Out-Null
Restart-Service sshd

Write-Output "sshd : $((Get-Service sshd).Status)"
Write-Output "Fin : $(Get-Date)"
