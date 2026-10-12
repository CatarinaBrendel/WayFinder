# Changelog

Notable completed changes to WayFinder.DevTools are recorded here. Entries are grouped by development milestone rather than release version; no release version is implied.

## Unreleased

### Milestone 16 — Git integration hardening (completed)

**Added**
- Read-only Git inspection exposed through MCP tools: `git_status`, `git_diff`, and `git_log`.
- Registered-project resolution for Git tools; validated repository-relative paths and literal Git pathspecs.
- Bounded stdout and stderr handling for Git subprocesses, with strict UTF-8 decoding.
- Regression coverage for output boundaries, Unicode handling, stderr overflow, and process termination.

**Security and reliability**
- Git is invoked with `ProcessStartInfo.ArgumentList`, not through a shell.
- Inherited `GIT_*` environment variables are removed from Git subprocess environments.
- Repository-configured `core.fsmonitor` is disabled; external diff and text conversion are disabled for diff operations.
- Git subprocesses use a 15-second timeout and are terminated on output overflow.
- Output budgets: branch/commit checks **4 KiB**, status **256 KiB**, log **64 KiB**, stderr **16 KiB**; diff stdout has a caller-specified limit and reports truncation.
- Git status may still invoke configured clean filters. Registered repositories must therefore be trusted; this is **not** a sandbox.

**Maintenance**
- Removed redundant output checks and an unused process-based reader overload.
- Removed unused process stream encoding configuration; byte streams are decoded explicitly.
- Normalized formatting in `GitCommandRunner`.

**Verification:** 342 tests passing; Git MCP integration previously exercised with Copilot CLI.

### Earlier implemented capabilities (before Milestone 16)

- Local-first .NET CLI and stdio MCP adapters over shared Application and Infrastructure services.
- Project discovery, artifact and technology detection, and optional `wayfinder.json` metadata.
- Registered-project access control for AI-facing tools, including exact-name and GUID project selection.
- Project-relative filesystem containment, repository search, bounded UTF-8 source reads, and targeted line ranges.
- Deterministic, token-budgeted context compilation.
- MCP tools `projects`, `context`, `repo_search`, and `repo_read`, exercised with Copilot CLI.

Earlier milestones are grouped here because their individual dates and release boundaries have not been established in this changelog.
