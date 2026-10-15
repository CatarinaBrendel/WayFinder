# WayFinder.DevTools Roadmap

WayFinder is a local-first, provider-neutral C#/.NET developer tooling platform. Its CLI and MCP server expose shared application capabilities, with registered-project boundaries for AI clients.

This document tracks **future direction and milestone status**. Architectural invariants and development rules belong in `AGENTS.md`; completed changes belong in `CHANGELOG.md`.

## Current status

- **Completed:** Milestone 16 Git integration and Milestone 17 Repository Intelligence (increments 17.1–17.6); interactive CLI and AI-conversation improvements (unreleased).
- **Next:** Not selected; agree on scope and security implications before beginning another milestone.
- **Verification:** Milestone 17 repository tests and the MCP stdio `tools/list` integration test were reported passing. A current full-suite test count and external Copilot/Claude validation of the new tools have not been supplied.

## Delivered capabilities

| Area | Status | Notes |
| --- | --- | --- |
| Foundation and project discovery | Implemented | Cross-platform .NET architecture; detector-based inspection. |
| Project filesystem boundary | Implemented | Project-relative access, containment enforcement, bounded reads. |
| Technology and artifact detection | Implemented | Extensible signatures and project manifests. |
| Project registry | Implemented | AI-facing access limited to explicitly registered projects. |
| Repository search and source retrieval | Implemented | Literal search, bounded whole-file reads, targeted line ranges. |
| Context compilation | Implemented | Deterministic, budgeted context packages. |
| MCP adapter | Implemented | Existing project/context/search/read and Git tools; five Repository Intelligence tools added in Milestone 17. |
| Git inspection | Completed (Milestone 16) | `git_status`, `git_diff`, `git_log`; read-only command allowlist and bounded subprocess execution. |
| Repository Intelligence | Completed (Milestone 17.1–17.6) | Solution/project discovery, project relationships, Roslyn C# symbols, source lines/declarations; CLI and MCP adapters. |
| Interactive CLI | Implemented (unreleased) | `wayfinder interactive`; local slash commands, AI conversation through a configured Copilot session, command/subcommand/option completion, terminal presentation and usage metrics. |

## Interactive CLI — delivered in the current iteration

- Natural-language input is forwarded to the configured conversation session; slash commands are handled locally and unknown slash commands are not forwarded to AI.
- Local commands: `/help`, `/status`, `/clear`, `/exit`, `/quit`, `/doctor`, `/project`, `/repo`, `/git`, `/tree`, and `/context` (service-backed commands depend on available services).
- Interactive Tab completion supports command names, subcommands, direct options, and subcommand options, with used-option filtering.
- The shell displays provider-aware startup guidance and distinguishes `Ready` from `Local mode`; AI responses have a distinct marker, model/token metrics, a spinner, and a session-scoped cursor style.
- Exit/quit display one goodbye message with a randomized developer-themed sign-off.
- **Follow-up:** Validate the complete test suite after the goodbye-output regression fix; do not treat a configured provider as a verified connectivity check.

## Milestone 17 — Repository Intelligence (completed: 17.1–17.6)

**Goal:** Help AI clients locate relevant code and understand repository structure without repeatedly reading whole files.

### Delivered increments

1. **17.1 — Solution and project discovery:** Discover `.sln`, `.slnx`, and .NET project files with project-relative results; discovery caps are 100 solutions and 500 projects.
2. **17.2 — Project relationships:** Inspect target frameworks, project references, and external assembly reference metadata (including `HintPath` and `CopyLocal`); do not inspect referenced DLL contents.
3. **17.3 — C# symbol discovery:** Use Roslyn to locate namespaces, types, and members, including qualified names and source line locations.
4. **17.4 — Targeted source navigation:** Read bounded line ranges or locate a declaration by qualified name, without requiring whole-file retrieval.
5. **17.5 — CLI integration:** `/repo discover`, `/repo relationships`, `/repo symbols`, `/repo source lines`, and `/repo source declaration`, including interactive completion.
6. **17.6 — MCP integration:** Expose `repo_discover`, `repo_relationships`, `repo_symbols`, `repo_source_lines`, and `repo_source_declaration` through the shared application services.

### Verification and boundaries

- The real MCP server was started over stdio by a test client; MCP initialization and `tools/list` succeeded, and all five new tool names were asserted.
- Repository operations remain read-only and project-relative; MCP tools resolve registered projects by registered name or GUID.
- Discovery excludes configured hidden/build directories and does not traverse directory symbolic links. Existing filesystem containment and bounded source-read policies remain in force.
- **Known follow-ups:** Discovery caps are enforced but truncation is not yet surfaced to callers. External Copilot/Claude use of the new MCP tools has not yet been verified. Filesystem symlink-chain and race-condition limitations still warrant separate review; do not describe the boundary as a complete sandbox.
- A fresh complete test-suite result has not been recorded in this document.

## Later directions — not scheduled

- Additional MCP-client validation, including Claude.
- Controlled execution capabilities with explicit authorization distinct from project registration.
- Project-specific intelligence, driven by demonstrated use cases.
- Token/context telemetry that distinguishes estimates from provider-reported usage.

These are candidates, not approvals to expand the AI-facing permission model.

## Working process

For each increment: **inspect → discuss → decide → implement → test → review**. Keep changes small and avoid speculative abstractions.
