#!/usr/bin/env bash
# Produit publish/CompteurRetraite.exe (Windows x64, un seul fichier, rien à installer).
# Usage : scripts/publier.sh <version> [proprietaire/depot]
#   version : par exemple 0.2.0. Elle doit être plus grande que celle déjà installée,
#             sinon le nouvel exe ne remplacera pas l'ancien (il ouvrira seulement les réglages).
#   dépôt   : active la mise à jour automatique depuis les releases GitHub de ce dépôt.
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

VERSION="${1:?Indiquez une version, par exemple : scripts/publier.sh 0.2.0}"
DEPOT="${2:-}"
dotnet publish src/CompteurRetraite.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:Version="$VERSION" -p:DepotGitHub="$DEPOT" -o publish
rm -f publish/*.pdb
ls -lh publish/CompteurRetraite.exe
