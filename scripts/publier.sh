#!/usr/bin/env bash
# Produit publish/CompteurRetraite.exe (Windows x64, un seul fichier, rien à installer).
# Usage : scripts/publier.sh [proprietaire/depot]   (le dépôt active la mise à jour automatique)
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

DEPOT="${1:-}"
dotnet publish src/CompteurRetraite.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DepotGitHub="$DEPOT" -o publish
rm -f publish/*.pdb
ls -lh publish/CompteurRetraite.exe
