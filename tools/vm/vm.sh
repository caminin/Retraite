#!/usr/bin/env bash
# Pilote la VM Windows de test.
#   tools/vm/vm.sh demarrer   lance la VM (premier lancement : téléchargement + installation, ~30 min)
#   tools/vm/vm.sh arreter    arrête proprement Windows
#   tools/vm/vm.sh etat       état et dernières lignes du journal
#   tools/vm/vm.sh supprimer  supprime le conteneur (le disque dans ~/VMs/compteur-retraite est conservé)
# L'écran de Windows est sur http://localhost:8006
set -euo pipefail
cd "$(dirname "$0")"
export DOCKER_CONTEXT=default   # moteur Docker natif (sudo systemctl start docker)

case "${1:-etat}" in
  demarrer)  mkdir -p "$HOME/VMs/compteur-retraite" ../../publish && docker compose up -d && echo "Écran : http://localhost:8006" ;;
  arreter)   docker compose stop ;;
  etat)      docker compose ps; docker logs --tail 15 compteur-windows 2>&1 || true ;;
  supprimer) docker compose down ;;
  *) echo "Usage : $0 demarrer|arreter|etat|supprimer"; exit 1 ;;
esac
