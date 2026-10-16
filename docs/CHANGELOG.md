# Changelog

Notable completed changes to WayFinder.DevTools are recorded here. Entries are grouped by development milestone rather than release version; no release version is implied.

## Unreleased

### Milestone 19 — C# syntax intelligence (local implementation, 2026-10-16)

**19.1 — C# member discovery**
- Expanded `repo_symbols` to report constructors, properties, fields, events,
  delegates and indexers; field/event declarations do not include local variables.
- Focused tests reported passing.

**19.2 — Declared type relationships**
- Added `repo_type_relationships`, a Roslyn syntax-only reader for declared
  base types and interfaces in a registered-project C# source file.
- Added 1 MiB input limit and 500-result limit with explicit truncation;
  type-shape, oversized-file and 500/501 boundary tests reported passing.
- Registered the reader in manual composition and added the tool to Explorer
  agent and launcher allowlists.

**19.3 — Syntactic reference discovery**
- Added `repo_references`, a read-only, case-sensitive identifier occurrence
  search across C# files using Roslyn `SimpleNameSyntax`.
- Returns source locations, syntax-based classifications, files examined,
  truncation and `Resolution: "Syntactic"`; it does not resolve symbol identity.
- Bounded to 1,000 examined files, 1 MiB/file and 200 references. Oversized or
  malformed UTF-16 files mark the search incomplete; shared decoder retains
  UTF-8, UTF-16 BOM and Windows-1252 fallback behavior.
- Registered the reader in composition and exposed an MCP tool. Explorer agent
  and launcher entries were provided for integration.
- **Verification reported:** Infrastructure and MCP builds and 11 focused
  reference-reader test cases green, including 200/201 references, 1,001 files,
  oversized source and malformed UTF-16 continuation.
- **Not yet evidenced:** Complete solution test result, final installation
  confirmation and end-to-end invocation of the new tools from Copilot CLI.

### Direction adopted — Copilot-first MCP platform (2026-10-16; documentation/planning)

**Decision — not a delivered runtime feature**
- Freeze **new development** of the `wayfinder interactive` conversation UX,
  input/editor, terminal presentation, completions, and ACP-specific UI.
  Preserve the implemented CLI and allow maintenance for security, correctness,
  compatibility and regression coverage; no removal is authorized.
- Make GitHub Copilot CLI the preferred conversation and native agent
  orchestration interface. Continue WayFinder as an independent, provider-neutral
  .NET Application/Infrastructure engine exposed through bounded, read-only
  stdio MCP tools.
- Select Milestone 18: (18.0) adopt updated documentation, (18.1) validate
  existing MCP tools end-to-end from Copilot, (18.2) prove read-only agent/tool
  containment, (18.3) develop task-focused C#/.NET/SQL/Sage instructions,
  (18.4) design safe SQL Server metadata access, and (18.5) evaluate workflows
  and controlled execution after security review.
- Treat Copilot's native shell/read/write and other configured tools as
  *separate trust boundaries* not controlled by WayFinder project registration.
- Keep legacy .NET Framework, Windows customer VMs, Sage 100/AppDesigner,
  and TFVC/TFS requirements in scope as **future domain support**, not
  delivered integrations.

**Verification status:** No Copilot end-to-end regression, new agent,
SQL Server tool, execution tool, or full-suite test run was performed as part
of this documentation-only direction decision. Existing results and gaps below
remain unchanged.

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
