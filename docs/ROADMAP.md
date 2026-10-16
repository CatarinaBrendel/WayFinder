# WayFinder.DevTools Roadmap

WayFinder is a local-first, provider-neutral C#/.NET developer tooling platform.
Its **preferred AI user interface is now GitHub Copilot CLI**; WayFinder supplies
safe, bounded, project-aware MCP tools and shared developer intelligence.

**Direction decision:** 2026-10-16. **Implementation status:** Copilot MCP
integration and a read-only Explorer workflow have since been implemented and
locally exercised. Security containment still requires separate verification.
The existing interactive CLI is preserved but frozen for new features.

This document tracks **future direction and milestone status**. Durable
architectural/security rules live in `AGENTS.md`. Delivered changes and
verified results live in `CHANGELOG.md`.

## Current status and decision

- **Completed in reported local development:** Milestones 16 and 17; M19.1
  C# member discovery, M19.2 declared type relationships, and M19.3 syntactic
  identifier reference discovery (implementation and focused tests).
- **Copilot integration:** A WayFinder Explorer agent, installation/registration
  scripts and explicit MCP tool allowlists have been configured. The new M19
  tools have been added to the agent and launcher. Copilot's native tools are
  a separate security boundary; an allowlist is not proof of OS-level isolation.
- **M19.3 verification:** Infrastructure and MCP builds and 11 focused
  reference-reader test cases were reported passing. The full-suite and final
  post-installation results were requested but **not explicitly confirmed** in
  the available conversation. Do not record them as verified.
- **Implemented but frozen:** `wayfinder interactive`; maintain for security,
  compatibility and regressions only.
- **Next:** confirm full-suite/deployment state, document residual security
  limitations, then prioritize the next bounded repository-intelligence task.

## Product boundaries

| Concern | Owner / direction |
| --- | --- |
| AI chat, conversation UX, model selection, session lifecycle | Copilot CLI (do not reimplement in WayFinder) |
| Agent invocation and delegation | Copilot-native agents first; no new WayFinder orchestrator yet |
| Agent guidance and repeatable processes | `AGENTS.md`, Copilot instructions, targeted agents/skills |
| Safe repository intelligence | WayFinder Application + Infrastructure behind stdio MCP |
| MCP authorization | Registered projects, bounded tools, typed validation, denial-by-default |
| Client-native shell/read/write permissions | Copilot/client and host sandbox; **not** protected by WayFinder registration |
| SQL Server / customer Windows VMs / Sage | Separate, explicitly authorized future domain integrations |
| CLI diagnostics | Keep headless utilities; freeze interactive conversation UI |

The server must remain usable by other compatible MCP clients; no core
Application/Infrastructure code should depend on Copilot-specific behavior.

## Delivered foundation (unchanged)

| Area | Status | Notes |
| --- | --- | --- |
| Cross-platform architecture | Implemented | .NET 10 WayFinder; macOS Apple Silicon primary dev host; customer code may target legacy .NET Framework. |
| Project registry and containment | Implemented | Registered-project access for MCP, project-relative reads; **not** a complete OS sandbox. |
| Project/technology detection | Implemented | Generic detector/signature model and optional `wayfinder.json`. |
| Search and source reads | Implemented | Literal bounded search, bounded UTF-8 whole-file/line reads. |
| Context compiler | Implemented | Deterministic budgeted packages; MCP default 4,000 estimated tokens. |
| MCP server | Implemented | Stdio transport; 14 tools in the current implementation inventory. |
| Read-only Git | Completed (M16) | `git_status`, `git_diff`, `git_log`; trusted-repository configuration caveat. |
| Repository intelligence | Completed (M17) | .NET discovery, relationships, Roslyn symbols, source lines and declarations. |
| Interactive CLI/ACP | Implemented; **frozen** | Existing features retained, no new UX roadmap. |

### Current MCP tool inventory (14)

`projects`, `context`, `repo_search`, `repo_read`, `git_status`, `git_diff`,
`git_log`, `repo_discover`, `repo_relationships`, `repo_symbols`,
`repo_source_lines`, `repo_source_declaration`, `repo_type_relationships`,
`repo_references`.

### M17 behavioral boundaries to preserve

- Discovery finds `.sln`, `.slnx`, and .NET project files (caps: 100
  solutions/500 projects); truncation metadata for discovery is still missing.
- Relationships inspect target frameworks, project references and external
  assembly reference metadata; referenced DLL contents are not loaded.
- Roslyn symbols and targeted source navigation are read-only and use
  project-relative names and bounded outputs.
- AI-facing selection uses registered exact name/GUID resolution; directory
  symlinks are excluded. Symlink-chain/race conditions remain security review
  concerns. Existing search/read/Git budgets remain defined in `AGENTS.md`.
- A real stdio MCP initialization/`tools/list` integration test passed;
  calling each new tool through an external Copilot/Claude client remains open.

## Milestone 18 — Copilot Integration & Secure Developer Tooling

### 18.0 — Product direction and documentation baseline

**Goal:** Formally record that Copilot CLI replaces *further development* of
WayFinder's custom interactive AI CLI as the preferred workflow.

**Scope:**
- Freeze interactive CLI UX/ACP feature development, preserve existing code.
- Maintain Architecture/Application/Infrastructure/MCP layering and read-only
  defaults; document provider-neutral vs Copilot-specific ownership.
- Update `AGENTS.md`, `ROADMAP.md`, `CHANGELOG.md` consistently.

**State:** Direction adopted. This document is an updated snapshot; confirm
repository incorporation independently. Documentation alone does not deliver
runtime functionality.

### 18.1 — Real Copilot ↔ WayFinder MCP baseline (partially verified)

**Goal:** Prove that current MCP functionality works reliably, securely and
with bounded cost inside the installed Copilot CLI.

**Work:**
1. Obtain a fresh `dotnet build`, `dotnet test`, and `git diff --check`
   result; address or record any failures, including the CLI regression.
2. Start the compiled MCP server over stdio and capture `initialize` and
   `tools/list` checks. Keep protocol stdout clean.
3. Register it in Copilot with a *limited explicit tool set* (no blanket
   permissions), checking the exact paths and tool names first.
4. From real Copilot sessions, exercise each of the 12 tools on a small known
   registered repository and record success, error and truncation behavior.
5. Verify failures for unregistered projects, ambiguous identifiers, `..`,
   absolute paths, symlink escape attempts, and invalid selectors.
6. Record tool response sizes, latency, actual Copilot usage when provided,
   and estimated context tokens **separately**.
7. Validate that native Copilot tools do not silently bypass the intended
   inspection-only workflow; use tool availability controls and host-level
   isolation as appropriate.

**Exit criteria:** all intended tools are discovered and successfully invoked
with evidence; denied cases fail closed; results are bounded and attributable;
remaining security assumptions and a complete test result are documented.

**Suggested bootstrap** (replace path with the actual compiled DLL):

```bash
copilot mcp add --tools projects,context,repo_search,repo_read,git_status,git_diff,git_log,repo_discover,repo_relationships,repo_symbols,repo_source_lines,repo_source_declaration wayfinder -- dotnet /absolute/path/to/WayFinder.DevTools.Mcp.dll
copilot mcp list
```

Inside Copilot use `/mcp show wayfinder`. Review available permissions and
**do not** interpret MCP registration as restricting Copilot's other tools.
GitHub documentation: https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-mcp-servers

### 18.2 — Read-only Copilot agent and permission policy (implemented; containment review open)

**Goal:** Provide a reproducible WayFinder Explorer that performs only bounded
inspection of registered projects.

**Work:**
- Create `.github/agents/wayfinder-explorer.agent.md` with a minimal explicit
  list of *verified* WayFinder MCP tools, rather than `*`.
- Define evidence-first output: selected project, tools used, relative
  files/lines, factual findings, uncertainties, and no mutations.
- Test agent invocation and delegation; confirm the parent Copilot session's
  native tools do not inherit fictitious WayFinder restrictions.
- Define a supported inspection-only launch/configuration with Copilot tool
  availability/deny rules **and**, where necessary, OS/container isolation.
- Model other MCP servers, filesystem reads, external processes, hooks and
  delegated agents as separate trust boundaries.

**Exit criteria:** allowlist validated in real use; disallowed actions cannot
be reached through the chosen inspection route; known residual risks recorded.

### 18.3 — Domain instructions and targeted skills

**Goal:** Make Copilot's analysis precise for the developer's environments
without bloating routine context.

**Work:**
- Use the root `AGENTS.md` as WayFinder project truth. Add a concise optional
  `.github/copilot-instructions.md` only where it adds distinct value.
- Prepare focused instructions/skills for C#, .NET Framework 4.8, SDK-style
  and old-style `.csproj`, DLL references, build/deployment constraints,
  Roslyn investigation, and safe code review.
- Add SQL Server/T-SQL investigation guidance that prioritizes metadata and
  explains production-sensitive operations; no DB access added by prompts.
- Add Sage 100 / AppDesigner and customer VM context without inventing APIs
  or assuming installed environments are accessible from macOS.
- Include TFVC/TFS as a future source-control integration case, distinct from
  already-implemented Git tools.
- Measure prompt/context size and whether the instructions improve outcomes.

**Exit criteria:** a small set of scenario tests demonstrate correct
framework/version inference, relevant bounded tool use, and fewer unsupported
assumptions; no security claim relies on instructions alone.

### 18.4 — SQL Server metadata intelligence (design gate before implementation)

**Goal:** Explore separately registered, explicitly approved SQL Server
instances/databases using least-privilege credentials.

**Design gate:** document credential handling, target allowlists, connection
transport, tenant/client separation, production controls, sensitive-data
redaction, timeouts and output limits; obtain explicit approval before coding.

**First permitted scope:** metadata discovery, object definitions and schema
relationships from approved catalogs, bounded outputs. Do **not** begin with
arbitrary SQL execution, stored-procedure invocation, or DDL/DML.

**Exit criteria:** least-privilege enforcement and denial tests on dedicated
non-production test databases, plus review of catalog metadata exposure.

### 18.5 — Coordinated agent workflows and controlled execution (later)

**Goal:** Use native Copilot agent delegation for explore → plan → review,
then consider explicitly authorized build/test tools.

**Candidates:** explorer, .NET specialist, SQL metadata specialist and reviewer;
roles should reuse WayFinder MCP APIs and have narrower permissions.

**Gate:** Do not implement a second orchestrator until measured failures of
Copilot-native delegation justify it. Do not add arbitrary shell, file-write,
Git mutation, SQL execution or deployment capabilities through M18 by default.

## Milestone 19 — C# syntax intelligence (implemented; verification noted below)

### 19.1 — C# member discovery

Extended `repo_symbols` to discover constructors, properties, fields, events,
delegates and indexers in addition to existing declarations. Field/event
variable discovery excludes local variables. Roslyn syntax-only parsing; no
compilation or semantic symbol binding. Focused tests reported passing.

### 19.2 — Declared type relationships

Added `repo_type_relationships` for declared class/interface/struct/record
base-type lists in a single registered-project C# file. Returns qualified
names, source locations and syntactically declared base types; does not resolve
actual type identities or load assemblies. Source size limit: 1 MiB; output
limit: 500 relationships with explicit `Truncated` status. Boundary tests at
500/501 and type-shape tests reported passing. Wired into composition and the
Explorer tool lists.

### 19.3 — Syntactic C# identifier references

Added `repo_references` for case-sensitive identifier occurrences across `.cs`
files within a registered project. Recognizes simple/generic names, invocations,
member access and object creation; excludes declaration identifiers. Returns
relative path, 1-based line/column, syntactic kind, files examined and
`Resolution: "Syntactic"`. It **does not** identify a unique semantic symbol,
resolve overloads or prove that matches refer to the same declaration.

Limits: up to 1,000 C# files examined, 1 MiB per file, and 200 returned
references. `Truncated` indicates an incomplete search due to limits, an
oversized file or an undecodable file. Decoding reuses the shared UTF-8,
UTF-16 BOM and Windows-1252 fallback reader; malformed UTF-16 is skipped with
truncation reported. Infrastructure and MCP builds and 11 focused test cases
reported green. MCP adapter, composition and Explorer/launcher entries were
added. **Full-suite and final installed-client verification remain unconfirmed.**

### Next verification gate

1. Run the complete solution test suite and `git diff --check`; record results.
2. Verify installation and that Copilot exposes both new M19 MCP tools.
3. Validate read-only tool restrictions separately from Copilot-native tools.
4. Review reference classification and truncation semantics before expanding
   scope (for example, file-read errors and unresolved semantic ambiguity).

## Post-M18 backlog — deliberately unscheduled

- TFVC/TFS read-only intelligence and Windows build/diagnostic adapters.
- Legacy .NET and Sage installation-aware inspection on approved customer VMs.
- Explicitly authorized build/test operations and audited mutation workflows.
- Optional Copilot UI extension/plugin distribution after core tool stability.
- Additional MCP clients, including Claude; portability/regression suite.
- Enhanced telemetry: tool calls, byte/time budgets, provider-reported usage.
- Truncation metadata for capped discovery and symlink/race hardening.

## Workflow and acceptance discipline

For each increment: **inspect → discuss → decide → implement → test → review**.
Keep changes small, ensure security tests cover denial paths, and update
`ROADMAP.md` and `CHANGELOG.md` with evidence **after** implementation.

A roadmap item is not delivered merely because a document, agent profile or
example configuration describes it. If validation cannot be performed, record
it as outstanding rather than claiming success.
