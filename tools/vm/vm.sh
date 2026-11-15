#!/usr/bin/env bash
# Pilote la VM Windows de test.
#   tools/vm/vm.sh demarrer   lance la VM (premier lancement : téléchargement + installation, ~30 min)
#   tools/vm/vm.sh arreter    arrête proprement Windows
#   tools/vm/vm.sh etat       état et dernières lignes du journal
#   tools/vm/vm.sh supprimer  supprime le conteneur (le disque dans ~/VMs/compteur-retraite est conservé)
#   tools/vm/vm.sh ssh [cmd]  ouvre PowerShell dans Windows (ou y exécute cmd)
#   tools/vm/vm.sh lancer <exe Windows> [arguments]
#                             lance un programme dans la session visible (comme un double-clic)
#   tools/vm/vm.sh taper <texte>
#                             tape du texte dans le champ qui a le focus (syntaxe SendKeys : {TAB}, {ENTER}…)
#   tools/vm/vm.sh diag      liste les fenêtres du bureau et du compteur (dans la session visible)
#   tools/vm/vm.sh installer-exe
#                             copie publish/CompteurRetraite.exe dans Téléchargements puis le lance
# L'écran de Windows est sur http://localhost:8006
set -euo pipefail
cd "$(dirname "$0")"
export DOCKER_CONTEXT=default   # moteur Docker natif (sudo systemctl start docker)
CLE="$HOME/VMs/compteur-retraite-ssh/cle_test"
OEM="$HOME/VMs/compteur-retraite-oem"

preparer() {
  mkdir -p "$HOME/VMs/compteur-retraite" ../../publish "$OEM" "$(dirname "$CLE")"
  [ -f "$CLE" ] || ssh-keygen -q -t ed25519 -N "" -C "vm-test-compteur-retraite" -f "$CLE"
  cp oem/install.bat oem/install.ps1 "$OEM/"
  cp "$CLE.pub" "$OEM/cle_test.pub"
}

scp_vm() {
  scp -q -i "$CLE" -P 2222 -o IdentitiesOnly=yes -o PreferredAuthentications=publickey -o BatchMode=yes \
      -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR "$@"
}

ssh_vm() { ssh -i "$CLE" -p 2222 -o IdentitiesOnly=yes -o PreferredAuthentications=publickey -o BatchMode=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR Docker@127.0.0.1 "$@"; }

# Exécute un script PowerShell local dans la VM (encodé, pour éviter les soucis de guillemets).
ps_vm() {
  local script="\$ProgressPreference = 'SilentlyContinue'
$1"
  ssh_vm "powershell -NoProfile -NonInteractive -EncodedCommand $(printf '%s' "$script" | iconv -t UTF-16LE | base64 -w0)"
}

# Les programmes lancés par SSH tournent hors de la session visible : on passe par une tâche planifiée interactive.
lancer_vm() {
  local exe="$1"; shift
  local args="${*:-}"
  local avecArgs=""
  [ -n "$args" ] && avecArgs="-Argument '$args'"
  ps_vm "\$a = New-ScheduledTaskAction -Execute '$exe' $avecArgs
\$p = New-ScheduledTaskPrincipal -UserId \$env:USERNAME -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName CompteurTest -Action \$a -Principal \$p -Force | Out-Null
Start-ScheduledTask -TaskName CompteurTest
Write-Output 'Lancé : $exe $args'"
}

case "${1:-etat}" in
  demarrer)  preparer && docker compose up -d && echo "Écran : http://localhost:8006" ;;
  arreter)   docker compose stop ;;
  etat)      docker compose ps; docker logs --tail 15 compteur-windows 2>&1 || true ;;
  supprimer) docker compose down ;;
  ssh)       shift; ssh_vm "$@" ;;
  lancer)    shift; lancer_vm "$@" ;;
  taper)     shift; lancer_vm powershell.exe "-NoProfile -WindowStyle Hidden -Command (New-Object -ComObject WScript.Shell).SendKeys(''$*'')" ;;
  diag)      scp_vm scripts/diag-fenetres.ps1 Docker@127.0.0.1:C:/Users/Public/diag-fenetres.ps1
             lancer_vm powershell.exe '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File C:\Users\Public\diag-fenetres.ps1' >/dev/null
             sleep 8
             ssh_vm 'Get-Content C:\Users\Public\diag.txt' ;;
  installer-exe)
             # Envoie publish/CompteurRetraite.exe dans Téléchargements (par SSH, plus fiable que le dossier partagé),
             # puis le lance dans la session visible, comme un double-clic.
             scp_vm ../../publish/CompteurRetraite.exe Docker@127.0.0.1:C:/Users/Docker/Downloads/CompteurRetraite.exe
             lancer_vm 'C:\Users\Docker\Downloads\CompteurRetraite.exe' ;;
  *) echo "Usage : $0 demarrer|arreter|etat|supprimer|ssh|lancer|taper|diag|installer-exe"; exit 1 ;;
esac
