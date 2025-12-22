#!/usr/bin/env bash
set -euo pipefail

RUNTIME="${1:-linux-x64}"

echo "Building backend (${RUNTIME})..."
dotnet publish ./backend/src/SlideGenerator.Presentation/SlideGenerator.Presentation.csproj \
  -c Release \
  -r "${RUNTIME}" \
  -o ./frontend/backend \
  --self-contained false

echo "Building frontend..."
cd ./frontend
npm install
npm run build

echo "Done."
