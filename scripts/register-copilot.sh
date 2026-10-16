#!/bin/sh
set -eu

MCP_DLL="$HOME/.local/share/wayfinder/mcp/WayFinder.DevTools.Mcp.dll"

MCP_TOOLS="projects,context,repo_search,repo_read,repo_discover,repo_relationships,repo_symbols,repo_source_lines,repo_source_declaration,git_status,git_diff,git_log"

if ! command -v copilot >/dev/null 2>&1; then
  echo "Error: GitHub Copilot CLI is not installed."
  exit 1
fi

if [ ! -f "$MCP_DLL" ]; then
  echo "Error: WayFinder MCP is not installed."
  echo "Run scripts/install.sh first."
  exit 1
fi

if copilot mcp list 2>/dev/null | grep -Eq '^[[:space:]]*wayfinder \(local\)'; then
  echo "WayFinder is already registered with Copilot."
  echo "Current configuration:"
  copilot mcp get wayfinder
  echo ""
  echo "To change this registration, run scripts/unregister-copilot.sh first."
  exit 0
fi

echo "Registering WayFinder MCP with Copilot..."

copilot mcp add \
  --transport stdio \
  --tools "$MCP_TOOLS" \
  --timeout 30000 \
  wayfinder -- \
  dotnet "$MCP_DLL"

echo ""
copilot mcp get wayfinder
