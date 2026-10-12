# WayFinder.DevTools Roadmap

WayFinder is a local-first, provider-neutral C#/.NET developer tooling platform. Its CLI and MCP server expose shared application capabilities, with registered-project boundaries for AI clients.

This document tracks **future direction and milestone status**. Architectural invariants and development rules belong in `AGENTS.md`; completed changes belong in `CHANGELOG.md`.

## Current status

- **Completed:** Git integration and hardening through Milestone 16.
- **Next:** Milestone 17 — Repository Intelligence (scope to be agreed before implementation).
- **Verification baseline:** 342 passing tests at completion of Milestone 16.

## Delivered capabilities

| Area | Status | Notes |
| --- | --- | --- |
| Foundation and project discovery | Implemented | Cross-platform .NET architecture; detector-based inspection. |
| Project filesystem boundary | Implemented | Project-relative access, containment enforcement, bounded reads. |
| Technology and artifact detection | Implemented | Extensible signatures and project manifests. |
| Project registry | Implemented | AI-facing access limited to explicitly registered projects. |
| Repository search and source retrieval | Implemented | Literal search, bounded whole-file reads, targeted line ranges. |
| Context compilation | Implemented | Deterministic, budgeted context packages. |
| MCP adapter | Implemented | `projects`, `context`, `repo_search`, `repo_read`. |
| Git inspection | Completed (Milestone 16) | `git_status`, `git_diff`, `git_log`; read-only command allowlist and bounded subprocess execution. |

## Milestone 17 — Repository Intelligence

**Goal:** Help AI clients locate relevant code and understand repository structure without repeatedly reading whole files.

Proposed increments (not yet committed implementation scope):

1. **Solution and project discovery:** Identify `.sln`/`.slnx` and project files, with compact project-relative results.
2. **Project relationships:** Identify references between .NET projects and expose a bounded dependency view.
3. **C# symbol discovery:** Find namespaces, types, and members with file paths and line locations.
4. **Targeted navigation:** Allow clients to move from a discovered symbol to a small source range using existing `repo_read`.
5. **MCP/CLI exposure:** Reuse shared application services; keep adapters thin and responses structured.

### Design constraints

- Read-only; only registered repositories are accessible to MCP clients.
- Reuse the existing project filesystem security boundary.
- Favor deterministic, bounded, project-relative results and incremental exploration.
- Do not introduce a vector index or Roslyn dependency without a demonstrated requirement and an explicit design decision.
- Agree on each increment's contract and tests before implementation.

### Definition of done (proposed)

- An AI client can identify a project or symbol and retrieve only the relevant source lines.
- Results are bounded, deterministic, and respect project access controls.
- CLI and MCP adapters share implementation logic.
- Tests, build, and external-client verification pass for the agreed scope.

## Later directions — not scheduled

- Additional MCP-client validation, including Claude.
- Controlled execution capabilities with explicit authorization distinct from project registration.
- Project-specific intelligence, driven by demonstrated use cases.
- Token/context telemetry that distinguishes estimates from provider-reported usage.

These are candidates, not approvals to expand the AI-facing permission model.

## Working process

For each increment: **inspect → discuss → decide → implement → test → review**. Keep changes small and avoid speculative abstractions.
