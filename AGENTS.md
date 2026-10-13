# WayFinder — Agent Instructions

WayFinder.DevTools is a local-first, provider-neutral developer tooling
platform built with C#/.NET 10.

It provides a CLI and MCP server for safe, structured access to software
projects.

## Documentation

- `docs/DEVELOPMENT_GUIDELINES.md`: Detailed architecture, security
  invariants, implementation conventions, and development policies.
- `ROADMAP.md`: Planned capabilities and development priorities.
- `CHANGELOG.md`: Completed changes and implementation history.

Read the relevant sections of `docs/DEVELOPMENT_GUIDELINES.md` before
making implementation or architectural changes.

Do not load the entire document for simple questions or read-only
project inspection.

## Architecture

- Keep application logic independent of CLI and MCP adapters.
- Keep WayFinder provider-neutral.
- Preserve cross-platform compatibility.
- Prefer small, focused changes over unnecessary abstractions.

## Security

- AI-facing project access is limited to explicitly registered projects.
- All project filesystem access must respect established security
  boundaries.
- Project registration grants read visibility, not write permission.
- MCP operations must remain read-only unless explicitly authorized
  through a separate permission model.
- Never expose unrestricted filesystem or shell execution.

Security requirements must be enforced in code, not merely through
agent instructions.

## Development Workflow

Follow this sequence:

1. Inspect the relevant implementation.
2. Discuss architectural or security decisions before changing them.
3. Implement the smallest useful increment.
4. Run relevant tests.
5. Review the changes.
6. Update ROADMAP.md and CHANGELOG.md when completing milestones.

Consult the detailed development guidelines whenever a change affects
security, architecture, public contracts, or implementation policy.

## Token Efficiency

- Prefer targeted repository searches.
- Read only the source sections needed for the task.
- Avoid unnecessary tool calls and large context dumps.
- Keep responses focused and accurate.
