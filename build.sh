#!/usr/bin/env bash
set -euo pipefail

RUNTIME="${1:-linux-x64}"

echo "Building backend (${RUNTIME})..."
dotnet publish ./backend/src/SlideGenerator.Presentation/SlideGenerator.Presentation.csproj \
  -c Release \
  -r "${RUNTIME}" \
  -o ./frontend/backend \
  --self-contained false

echo "Building frontend (no publish)..."
cd ./frontend
npm install
export ELECTRON_BUILDER_PUBLISH=never
npm run build
unset ELECTRON_BUILDER_PUBLISH

echo "Done."
