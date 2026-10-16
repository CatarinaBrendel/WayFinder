# WayFinder documentation handoff — 2026-10-16

This bundle contains updated versions of the **three existing primary
repository documents**:

1. `AGENTS.md` — existing architecture and security requirements retained;
   documents Copilot-first strategy, legacy interactive CLI freeze, agent
   boundaries and targeted .NET/C#/SQL Server/Sage/TFVC guidance.
2. `ROADMAP.md` — records the accepted new direction and an explicit M18.0–18.5
   plan, including acceptance criteria, gates and unresolved verification.
3. `CHANGELOG.md` — logs the 2026-10-16 **documentation/product decision**
   without inventing a completed runtime release.

## How to use

- **For the source repository:** review the diff, then replace the current
  root `AGENTS.md`, `ROADMAP.md`, and `CHANGELOG.md` with these files. Do not
  silently overwrite unrelated modifications made after the source snapshot.
- **To resume work in ChatGPT:** upload these three Markdown files (or the ZIP)
  and ask to begin **M18.1 — Copilot MCP end-to-end baseline**. If the files
  are not yet incorporated into the repository, indicate that they are a
  proposed documentation update, not an implementation milestone.

## Baseline

Source documents reviewed: `AGENTS(20261015-090327).md`, `ROADMAP(5).md`,
`CHANGELOG(5).md`. The original completed M16/M17 capabilities and security
constraints were retained. No repository source code was accessed or changed;
no builds/tests were executed.

Copilot feature syntax and security controls were checked against GitHub Docs
on 2026-10-16. Exact use and enforcement must still be validated against the
locally installed CLI.

- MCP registration: https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-mcp-servers
- Custom instructions: https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-custom-instructions
- Tools/permission controls: https://docs.github.com/en/copilot/how-tos/copilot-cli/use-copilot-cli/allowing-tools
- Custom agents: https://docs.github.com/en/copilot/how-tos/copilot-cli/use-copilot-cli/invoke-custom-agents
