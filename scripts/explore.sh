#!/bin/sh
set -eu

copilot \
  --agent wayfinder-explorer \
  --disable-builtin-mcps \
  --available-tools='wayfinder-projects,wayfinder-context,wayfinder-repo_discover,wayfinder-repo_search,wayfinder-repo_read,wayfinder-repo_relationships,wayfinder-repo_symbols,wayfinder-repo_source_lines,wayfinder-repo_source_declaration,wayfinder-git_status,wayfinder-git_diff,wayfinder-git_log' \
  --allow-tool='wayfinder' \
  "$@"

EXIT_CODE=$?

if [ "$EXIT_CODE" -eq 0 ]; then
  printf '\n'

  case $(($(od -An -N4 -tu4 /dev/urandom | tr -d ' ') % 8)) in
    0) echo "WayFinder: Another day, another dependency graph survived." ;;
    1) echo "WayFinder: The code remains. Your patience is appreciated." ;;
    2) echo "WayFinder: Logging off before someone mentions a rewrite." ;;
    3) echo "WayFinder: May your builds be green and your diffs intentional." ;;
    4) echo "WayFinder: No repositories were harmed during this session." ;;
    5) echo "WayFinder: Remember, legacy code is just code with experience." ;;
    6) echo "WayFinder: The technical debt will still be here tomorrow." ;;
    7) echo "WayFinder: Session complete. The bugs have been left unsupervised." ;;
  esac
fi

exit "$EXIT_CODE"
