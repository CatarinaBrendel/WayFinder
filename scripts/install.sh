#!/bin/sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"

PROJECT="$ROOT_DIR/src/WayFinder.DevTools.Mcp/WayFinder.DevTools.Mcp.csproj"
INSTALL_DIR="$HOME/.local/share/wayfinder/mcp"

echo "Publishing WayFinder MCP..."

dotnet publish "$PROJECT" \
  --configuration Release \
  --output "$INSTALL_DIR"

echo ""
echo "WayFinder MCP installed:"
echo "  $INSTALL_DIR/WayFinder.DevTools.Mcp.dll"
