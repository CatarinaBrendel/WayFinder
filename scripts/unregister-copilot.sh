#!/bin/sh
set -eu

if ! command -v copilot >/dev/null 2>&1; then
  echo "Error: GitHub Copilot CLI is not installed."
  exit 1
fi

if ! copilot mcp get wayfinder >/dev/null 2>&1; then
  echo "WayFinder is not registered."
  exit 0
fi

echo "Removing WayFinder MCP registration..."

copilot mcp remove wayfinder

echo "WayFinder registration removed."
