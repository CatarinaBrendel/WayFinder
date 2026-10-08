#!/bin/sh

set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
PROJECT="$ROOT_DIR/src/WayFinder.DevTools.Cli/WayFinder.DevTools.Cli.csproj"
INSTALL_DIR="$HOME/.local/share/wayfinder"
BIN_DIR="$HOME/.local/bin"
COMMAND="$BIN_DIR/wayfinder"

echo "Publishing WayFinder..."

dotnet publish "$PROJECT" \
  --configuration Release \
  --output "$INSTALL_DIR"

mkdir -p "$BIN_DIR"

ln -sf "$INSTALL_DIR/WayFinder.DevTools.Cli" "$COMMAND"

echo "Installed WayFinder:"
echo "  $COMMAND"
