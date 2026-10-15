# Changelog

Notable completed changes to WayFinder.DevTools are recorded here. Entries are grouped by development milestone rather than release version; no release version is implied.

## Unreleased

### Milestone 17 — Repository Intelligence (completed: 17.1–17.6)

**Added**
- Solution and .NET project discovery (`.sln`, `.slnx`, project files), bounded to 100 solutions and 500 projects.
- Project relationship inspection: target frameworks, project references, and external assembly metadata (`HintPath`, `CopyLocal`) without loading external DLLs.
- Roslyn-backed C# namespace/type/member discovery with qualified names, project-relative paths, and line locations.
- Targeted source navigation by line range and qualified declaration name.
- CLI commands `/repo discover`, `/repo relationships`, `/repo symbols`, `/repo source lines`, and `/repo source declaration`, with interactive completion.
- MCP tools `repo_discover`, `repo_relationships`, `repo_symbols`, `repo_source_lines`, and `repo_source_declaration`, implemented as adapters over shared services.

**Security and verification**
- MCP tools require registered-project resolution; source access retains project-relative containment, read-only behavior, and existing bounded reads.
- Directory symlink traversal and configured hidden/build directories remain excluded from discovery.
- Repository Intelligence unit/integration tests were reported green. A protocol-level integration test launched the actual stdio MCP server and asserted all five new tools through `tools/list`.
- Discovery result caps do not currently expose truncation metadata. External Copilot/Claude verification of the new tools and a final full-suite count remain outstanding.

### Interactive CLI and AI conversations (implemented; milestone number not assigned)

**Added**
- `wayfinder interactive` session with direct natural-language conversations through a configured GitHub Copilot conversation session; slash commands remain local.
- Interactive commands for help, status, clear, exit/quit, diagnostics, project management, repository reading/search, Git inspection, tree view, and context compilation (subject to service availability).
- Tab completion for command names, subcommands, subcommand options, and direct options; completion suggestions and used-option filtering.
- AI response presentation with distinct AI and model-usage markers, input/output token counts, cache percentage, and elapsed time when supplied by the provider.

**Terminal experience**
- Provider-aware welcome hint and `Ready`/`Local mode` startup and status presentation.
- Consistent prompt spacing, indented animated thinking spinner, and separate presentation markers for WayFinder, AI replies, and metrics.
- Blinking bar cursor requested for the interactive terminal session, with terminal-default restoration on normal exit.
- Randomized developer-themed goodbye messages, with a stable `Session ended.` heading and a single exit presentation.

**Verification**
- Build and tests were reported green during completion development.
- A subsequent run reported 415 passed and one failure because an existing test asserted `Session ended.` after the goodbye wording changed. The presentation was adjusted to retain that stable heading; a final full-suite result has not been supplied.


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
