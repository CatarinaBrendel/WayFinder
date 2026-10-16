# AGENTS.md

## Project Overview

WayFinder.DevTools is a local-first developer tooling platform written
in C#/.NET.

Its purpose is to provide safe, efficient, structured access to software
projects for:

- AI clients (initially GitHub Copilot CLI) through an MCP server.
- Compatible AI clients, including Claude and other MCP consumers.
- Human developers through existing headless `wayfinder` commands where useful.
- Future project-specific developer tooling.

**Direction adopted 2026-10-16:** GitHub Copilot CLI is the preferred
conversation/agent interface. The WayFinder interactive CLI is frozen for new
features, but existing code is preserved. WayFinder differentiates itself
through safe, bounded MCP capabilities, repository intelligence, domain-aware
instructions, and reusable workflows—not through a competing chat terminal.

The CLI and MCP server are adapters over shared application services.
Business logic must not be duplicated between them.

WayFinder remains provider-neutral: core contracts and services must not
depend on GitHub Copilot or another specific AI provider. Copilot-specific
profiles, hooks, and packaging belong in integration files or adapters.

---

## Product Direction and CLI Freeze (effective 2026-10-16)

**Maintain, do not expand, the existing `wayfinder interactive` experience.**

Frozen development areas:

- Interactive conversation UX, prompt/editor, completion, spinners, cursor,
  terminal rendering, and new interactive slash commands.
- ACP presentation and provider-session features designed primarily to compete
  with Copilot CLI's native conversation and agent experience.
- Additional terminal UI frameworks, custom agent chat shells, and duplicate
  session/agent orchestration engines.

Allowed maintenance:

- Security fixes, correctness regressions, compatibility fixes, and tests that
  preserve the existing experience.
- Headless/diagnostic CLI commands needed for local administration, tests, or
  the MCP architecture; these must not create another AI conversation UI.
- Removal/deprecation work only after an explicit, separately reviewed decision.

Prioritize the MCP server and shared Application/Infrastructure services,
secure project and database boundaries, Copilot-native agent profiles, skills,
and concise domain instructions. Do not delete the CLI or ACP code as part of
this direction change.

**State distinction:** This is an accepted product direction and documentation
update; it does not imply that Copilot integration, new agents, SQL tools, or
other planned features have already been implemented.

---

## Project Documentation

The repository maintains three primary documents:

- `AGENTS.md`: Architecture, security invariants, coding conventions,
  and instructions for AI agents.
- `ROADMAP.md`: Planned capabilities, development milestones,
  priorities, and future directions.
- `CHANGELOG.md`: Completed changes and implementation history.

AI agents should consult these documents before proposing
architectural changes or beginning a new milestone.

Documentation responsibilities:

- Keep durable engineering rules in `AGENTS.md`.
- Maintain development plans and milestone status in `ROADMAP.md`.
- Record completed changes in `CHANGELOG.md`.
- Avoid duplicating milestone histories or test counts in `AGENTS.md`.

When completing a milestone, update `ROADMAP.md` and `CHANGELOG.md`
before committing.

---

## Development Environment

WayFinder is developed primarily on macOS and must remain cross-platform
unless a feature explicitly requires otherwise.

Current baseline:

- .NET 10
- C#
- macOS on Apple Silicon
- VS Code
- xUnit

Avoid Windows-specific APIs, filesystem assumptions, path separators,
environment variables, or shell behavior unless isolated behind an
explicit platform abstraction.

Use `Path`, `DirectoryInfo`, and other cross-platform .NET APIs instead
of manually constructing filesystem paths.

---

## Architecture

The solution is divided into these primary projects:

```text
WayFinder.DevTools.Application
WayFinder.DevTools.Infrastructure
WayFinder.DevTools.Cli
WayFinder.DevTools.Mcp
```

The CLI and MCP projects are sibling adapters over the same Application
and Infrastructure services. Neither adapter should call the other.

Dependencies should flow toward the application layer.

Conceptually:

```text
CLI ─────────┐
             ▼
        Application
             ▲
MCP ─────────┘
             │
             ▼
       Infrastructure
```

### Application

Defines WayFinder capabilities and application-level abstractions.

Examples:

- project context
- project inspection
- project detectors
- project filesystem contracts
- project registry and registered-project resolution contracts
- repository search contracts
- bounded repository source retrieval contracts
- deterministic context compilation contracts

Application code should not depend directly on operating-system
filesystem or process APIs when an abstraction is appropriate.

### Infrastructure

Implements interaction with the host environment.

Examples:

- filesystem access
- Git
- dotnet CLI
- process execution
- project discovery
- project artifact detection

Infrastructure must enforce security boundaries required by the
Application contracts.

### CLI (existing adapter; maintenance-only for interactive features)

The CLI is a human-facing adapter.

It should contain:

- command definitions
- argument parsing
- human-readable presentation
- composition/wiring

It should contain as little business logic as possible.

### Interactive CLI and conversation presentation (legacy behavior)

The following rules document **existing code and regression guarantees**;
they are not an authorization for new interactive UX features.

The `wayfinder interactive` shell is a human-facing adapter. It owns the
interactive session lifecycle, dispatches recognized slash commands to
registered handlers, and forwards ordinary text to a configured
`ConversationSession`. Unknown slash commands must **not** be forwarded
to the AI provider. With no configured conversation session, local
commands remain usable and natural-language input receives a clear
unavailable-provider message.

Keep responsibilities separate:

- `InteractiveShell`: session lifecycle, provider-aware status, and
  dispatch between local commands and conversations.
- `InteractiveLineEditor`: terminal input, redraw, and Tab suggestion UI;
  do not add blank lines to per-keystroke prompt redraws.
- `InteractiveCommandCompleter` and optional
  `IInteractiveCommandCompletion`: command, subcommand, and option
  completion. Suggestions must preserve the full input prefix and avoid
  repeating already-used options.
- `InteractivePresentation`, `ConsolePresenter`, and `ConsoleTheme`:
  consistent human-facing output, AI/metrics markers, and goodbye text.
- `ConsoleSpinner`: short-lived animation and line cleanup while a
  provider request is in progress.

Terminal-only ANSI behavior (including session-scoped blinking cursor
style) must be gated on actual interactive console input/output, and
restored in `finally` when the session ends. Tests and redirected streams
must not receive terminal-control sequences. A configured provider
indicates availability of a session, not a successful remote health check.

Keep goodbye rendering in one place in the shell's exit path, not both
in an exit command and the shell. Retain a stable `Session ended.` label
for testable output even when the accompanying message is randomized.

### ACP conversation and permission lifecycle (frozen, maintain for compatibility)

The existing `WayFinder.DevTools.Cli` interactive shell and ACP integration
are preserved for compatibility and regression testing, **not** the preferred
new AI conversation interface. GitHub Copilot CLI is preferred for new use.
Do not replace the frozen shell with a TUI framework or maintain another
terminal UI without an explicit design decision.

- `ConversationSession` coordinates a provider turn; `CopilotAcpProvider`
  adapts Copilot ACP, while `AcpClient` owns ACP JSON-RPC transport.
- A `session/request_permission` request is **part of an active turn**, not
  a completed turn or a new user message. Keep the original streaming
  operation pending while collecting the decision.
- Match each permission response to the incoming JSON-RPC request ID and
  return only an option offered by the provider. Never assume permission
  was granted when the handler is unavailable or fails.
- Serialize interactive permission prompts so only one component reads
  terminal input at a time. Preserve Allow once, Deny, and Escape-to-deny
  behavior; cancellation and shutdown must not leave a request hanging.
- The CLI owns human-facing confirmation and rendering. The provider/ACP
  layer owns protocol messages. Neither layer should duplicate the other.
- Preserve existing streaming, tool-status reporting, prompt editing,
  autocomplete, and redirected-stream behavior when adding interactions.
- Coordinate spinner and streaming output with permission prompts; avoid
  simultaneous writes that corrupt terminal rendering. Wrapped streamed
  lines must retain the conversation prefix.
- ACP confirmation is a user-interaction mechanism, **not** a replacement
  for project containment, read-only restrictions, or tool authorization.
- Add regression tests for permission approval, denial, missing handler,
  concurrent requests, cancellation, and continuation of the same turn.

### MCP

The MCP server is an AI-facing adapter over the same Application and
Infrastructure services used by the CLI.

The current read-only MCP tool surface is:

```text
projects
context
repo_search
repo_read
git_status
git_diff
git_log
repo_discover
repo_relationships
repo_symbols
repo_source_lines
repo_source_declaration
```

MCP tools must remain thin adapters. Validation and response mapping
that are specific to the protocol belong in MCP; repository behavior,
security policy, context selection, and other reusable business logic
belong in shared Application or Infrastructure services.

The MCP server uses stdio transport. Treat the streams as a protocol
boundary:

```text
stdin   MCP protocol input
stdout  MCP protocol output
stderr  diagnostics and logging
```

Never write human-readable diagnostics to stdout from the MCP process.

---

## Copilot-first Integration Rules

The primary integration target is **GitHub Copilot CLI using WayFinder over
local stdio MCP**, with Copilot's built-in conversation, model selection, and
agent delegation retained. Provider-neutral MCP contracts remain the product's
stable interface; avoid assuming a particular model or paid API key.

Use the following separation of responsibilities:

- **Copilot CLI:** conversation UX, model selection, session lifecycle,
  agent selection/delegation, and native per-session tool permissions.
- **WayFinder MCP:** narrow, documented, typed tools, compact structured
  results, registered-project resolution, authorization and resource bounds.
- **WayFinder Application/Infrastructure:** reusable discovery, source
  navigation, context, Git, and future approved domain operations.
- **Copilot agent profiles / skills / instructions:** role selection,
  working procedure, code-style and domain guidance; never a security boundary.

Initial integration contract:

1. First validate the existing 12 MCP tool names, inputs and outputs with
   *actual* Copilot CLI calls; protocol `tools/list` alone proves discovery,
   not correct external behavior.
2. First custom agent is a read-only **WayFinder Explorer** that accesses only
   the explicitly permitted WayFinder MCP tools and reports evidence.
3. Keep agent responsibilities narrow; defer new multi-agent coordinator code.
   Prefer Copilot's native delegation and test how delegation handles tool
   restrictions and inherited instructions.
4. Prefer existing `AGENTS.md` for authoritative repository rules. Add short
   `.github/copilot-instructions.md` or path-specific `*.instructions.md` only
   when actually necessary; avoid conflicting duplicate rules. Keep optional
   skills task-scoped and loaded on demand.
5. Document tested tool availability, failures, truncation, timeouts, and
   token/byte costs before attempting further automation.
6. Copilot CLI extensions/plugins and UI embellishments are optional, later
   work—not prerequisites for a secure MCP integration.

Copilot supports custom agent profiles under `.github/agents/` or the user
`~/.copilot/agents/` directory and per-project skills/instructions. Version
and exact syntax must be verified against the installed Copilot CLI; do not
invent support for undocumented UI or agent interception APIs.

### Access-control and trust boundaries

**WayFinder registration only constrains calls through WayFinder.** Copilot
may independently offer native read/write/shell, web, GitHub, memory,
other MCP servers, or delegated agents; those are separate authorities.

- A prompt telling an agent to avoid a tool is *not* an authorization check.
- For an inspection-only session, restrict the tools presented to the agent
  (for example, Copilot's `--available-tools`), deny shell/write and other
  unrelated capabilities where supported, and verify actual enforcement.
- A custom agent's `tools` allowlist reduces that agent's accessible tools
  but must not be mistaken for a complete OS sandbox, nor an automatic
  restriction on Copilot's parent conversation.
- Never use `--allow-all-tools` (or equivalent blanket approval) as a
  standard WayFinder integration recipe.
- Use host-level isolation/least privilege when customer repositories,
  credentials, or databases require stronger guarantees.
- Delegated agents, hooks, plugins, and subprocesses must be considered in
  the threat model. Do not let them bypass the registered-project resolver.
- Stdio protocol output must remain uncontaminated and free of secrets;
  connection strings, credentials, access tokens, and customer data must not
  be logged indiscriminately.

### Capability tiers (planned policy)

1. **Inspect** — default: read-only, registered-repository MCP access; any
   future database access requires separate explicit registration and
   least-privilege database credentials.
2. **Execute** — future: tightly scoped build/test or diagnostic operations
   with explicit authorization, fixed executable/argument contracts, bounded
   processes, and no generic shell tool.
3. **Modify** — future, separate review: file, Git, TFVC, configuration, or
   database changes require authorization beyond registration and suitable
   verification or rollback procedures.

The tiers describe desired policies, **not** a claim that mode switching or
new execution/write tools are implemented.

---

## Project Access Security

Project isolation is a core security invariant.

AI-facing operations may access only files contained within projects
explicitly made available to WayFinder.

The project filesystem boundary is represented by:

```text
IProjectFileSystem
```

AI-facing project functionality must not bypass this abstraction with
direct calls to:

```text
File
Directory
FileInfo
DirectoryInfo
```

when accessing project contents.

Infrastructure implementations may use these APIs internally to
implement the boundary.

### Path Rules

Project-facing paths should normally be project-relative.

Do not expose arbitrary absolute filesystem paths to AI-facing APIs.

Reject attempts to escape a project through:

```text
../
absolute paths
filesystem links resolving outside the project
```

Canonical containment must be verified before exposing filesystem
content.

Directory symbolic links are not traversed by the V1 project filesystem
implementation.

Default to denying additional filesystem capability unless there is a
demonstrated need for it.

---

## Project Registry Security

WayFinder will support two project-discovery models.

### CLI

The human CLI may discover the Git repository containing the current
working directory.

This allows commands such as:

```text
wayfinder project info
```

without requiring prior registration.

### MCP / AI Clients

AI clients should only receive access to projects explicitly registered
with WayFinder.

An AI client must not be able to recursively discover arbitrary
repositories or directories elsewhere on the user's computer.

Registering a project grants visibility to that project. It must not
automatically imply unrestricted write access.

WayFinder's own internal configuration storage is separate from the
project filesystem exposed to AI clients.

---

### Git Repository Trust and Execution Security

WayFinder operates exclusively on repositories explicitly registered
and trusted by the user.

Security requirements:

- MCP clients and AI agents must not access unregistered repositories.
- Git operations exposed through MCP must remain read-only.
- Git commands must use structured process arguments without shell execution.
- Git-specific environment variables (`GIT_*`) must not be inherited.
- Repository-configured filesystem monitors must be disabled.
- External diff programs and text-conversion drivers must be disabled
  for Git diff operations.
- Git subprocesses must enforce execution timeouts and output limits.

#### Trusted Repository Configuration

Git may execute repository-configured content filters during certain
operations, including `git status`.

This behavior is accepted for explicitly registered, trusted repositories
to preserve compatibility with legitimate Git workflows.

Consequently:

- Repository registration establishes a trust boundary.
- Users must register only repositories whose configuration and contents
  they trust.
- Read-only Git commands do not guarantee that Git executes no external
  programs.
- AI agents must not be allowed to modify repository configuration or
  execute arbitrary Git commands through WayFinder.

WayFinder's Git integration provides controlled read-only access,
not a complete operating-system sandbox.

---

## Filesystem Access

Avoid creating a generic filesystem service exposed to AI clients.

Do not expose operations equivalent to:

```text
read arbitrary file
write arbitrary file
delete arbitrary file
list arbitrary directory
```

AI-facing source reading is bounded through repository-specific
operations.

The current `repo_read` behavior is text-only and project-relative. The
repository reader owns its read policy; adapters must not duplicate its
limits.

Current V1 behavior supports two read modes.

Whole-file reads:

- maximum returned source content is 64 KiB;
- total byte count and truncation state are reported;
- NUL-containing files are treated as unsupported binary content;
- malformed UTF-8 is rejected;
- a UTF-8 BOM is accepted and removed;
- when truncation cuts through a UTF-8 sequence, the incomplete
  sequence is omitted safely.

Targeted range reads:

- use 1-based `startLine` values consistent with `repo_search`;
- accept `startLine` and `lineCount` together;
- return at most 500 requested lines;
- return the requested start line and the actual final returned line;
- use a null end line when the requested range is beyond EOF;
- preserve original line endings;
- preserve UTF-8 characters that cross internal byte-buffer boundaries;
- apply the same strict UTF-8 and NUL/binary validation as whole-file reads.

AI clients investigating a known code location should prefer:

```text
repo_search
→ targeted ranged repo_read
```

over reading an entire file.

Large files must not be returned automatically. Do not expose an
arbitrary filesystem read primitive to work around repository read
limits.

---

## Shell and Process Execution

Do not expose a generic MCP tool equivalent to:

```text
shell(command)
```

or:

```text
execute(command)
```

Instead expose constrained operations with explicit semantics.

Examples:

```text
git_status
git_diff
dotnet_build
dotnet_test
repo_search
repo_read
```

Each operation should define:

- allowed inputs
- execution scope
- timeout
- output limits
- structured result format

Prefer deterministic operations over arbitrary command execution.

---

## Project Inspection

Project inspection is detector-based.

Each detector implements:

```text
IProjectDetector
```

Detectors should identify factual project artifacts rather than making
unnecessary assumptions.

Examples:

```text
dotnet
  solution
  project
  globaljson

guidance
  agents
  editorconfig
```

Projects may be polyglot.

Do not model a repository as belonging to exactly one technology.

A repository may simultaneously contain:

```text
.NET
Node
Tauri
Rust
Python
other technologies
```

Adding a detector should not require modifying generic inspection or
rendering logic.

Detectors accessing project contents must use `IProjectFileSystem`.

---

## Detection vs Interpretation

Keep these concepts separate:

```text
Detection
    What exists?

Interpretation
    What does it mean?

Presentation
    How should it be displayed?
```

Prefer factual detection first.

Do not introduce large central technology enums or models that require
WayFinder to know every possible ecosystem in advance.

---

## Technology Model

WayFinder is technology-agnostic and must not assume that its built-in
technology catalog represents the complete universe of technologies.

Technology identifiers are open-ended strings, not enums or other closed
type systems.

Built-in technology recognition is data-driven through the embedded
`technologies.json` catalog. Adding recognition for another ecosystem
should normally require adding or changing catalog data, not
implementing another technology-specific detector.

The generic `TechnologySignatureDetector` interprets technology
signatures. Do not create detectors such as `SwiftProjectDetector`,
`PythonProjectDetector`, or `RustProjectDetector` when the technology
can be described through signatures.

Special-purpose detectors remain appropriate when behavior cannot
reasonably be represented by technology signatures.

Project manifests may declare technologies that are unknown to
WayFinder. Unknown technology identifiers are valid project metadata and
must not be rejected.

Keep these concepts separate:

- `technologies.json` describes technologies WayFinder knows how to
  recognize.
- `wayfinder.json` describes technologies a particular project
  declares that it uses.
- Detection represents repository evidence.
- Declaration represents developer-provided project knowledge.
- AI consumers may understand technologies that WayFinder itself does
  not recognize.

WayFinder must remain useful for projects that do not use .NET, Node,
DeadRoute-specific conventions, or any other particular ecosystem.

## Project Manifest

A repository may contain a root-level `wayfinder.json`.

The manifest is optional for normal CLI inspection but will provide
explicit project metadata and may later participate in AI authorization
and context generation.

Manifest version 1:

- `version` is required and must be `1`.
- `name` is optional.
- `technologies` is required and may be empty.
- Technology identifiers must be non-empty.
- Duplicate technology identifiers are invalid.
- Unknown technology identifiers are valid.
- Extra JSON properties should remain tolerated for forward
  compatibility.

Reading project manifests must go through `IProjectFileSystem`. Do not
bypass the project filesystem security boundary with direct arbitrary
file access.

Commands named `info`, `inspect`, `list`, `show`, or similar
observational operations must remain read-only. The explicit
`project init` operation may create `wayfinder.json`; initialization is
a controlled human-facing write and must not be inferred from
observational commands.

## Project Registration and AI Authorization

Project registration is a security boundary.

The human CLI may inspect the Git repository containing the current
working directory without requiring registration.

AI-facing adapters, including MCP, must only expose projects that have
been explicitly registered with WayFinder.

Registration grants AI-facing adapters read visibility to a project. It
does not grant permission to modify the project.

A registered project may be:

- inspected;
- searched;
- read through bounded WayFinder read operations;
- used to construct project context.

Registration alone must never permit:

- creating files;
- modifying files;
- deleting files;
- executing arbitrary commands;
- changing Git state;
- committing or pushing;
- installing dependencies;
- running arbitrary scripts.

Any future AI-facing mutation capability must use a separate explicit
authorization model. Write authorization must never be inferred from
project registration.

All AI-facing filesystem operations must continue to pass through
WayFinder's project filesystem security boundary.

The project registry must not provide a mechanism for escaping
project-root containment.

AI-facing tools must resolve project access through the
registered-project resolver before invoking repository operations. MCP
tools that operate on a registered project accept either its authoritative
GUID or its exact registered name. Exact-name matching is ordinal
case-insensitive; GUID-shaped input does not fall back to name lookup, and
ambiguous duplicate names must be rejected. Arbitrary filesystem roots are
never accepted as project selectors.

## CLI Output

Human-facing CLI output may be formatted for readability.

Generic inspection output should remain capable of representing
detectors unknown to the CLI.

Avoid hardcoding rendering logic such as:

```text
WriteDotNetSection
WriteNodeSection
WriteRustSection
```

when generic artifact rendering is sufficient.

Machine-facing MCP output should prefer structured data over prose.

---

## Determinism

WayFinder should produce deterministic results wherever practical.

Stable ordering is preferred for:

- files
- search results
- project artifacts
- Git output
- structured MCP responses

Deterministic output improves:

- reproducibility
- testing
- diff quality
- caching
- AI context stability
- token efficiency

Do not depend on filesystem enumeration order.

---

## Token Efficiency

MCP itself does not make AI usage token-efficient.

WayFinder must actively control the amount of context returned to AI
clients.

Repository operations should support bounded output.

Examples include:

- maximum result count
- maximum lines
- maximum bytes
- targeted file ranges
- search snippets
- truncation metadata
- continuation mechanisms

Prefer:

```text
small structured result
```

over:

```text
large repository dump
```

Never send an entire repository to an AI model as a default context
strategy.

Future telemetry should distinguish between:

- operation duration
- output bytes
- approximate emitted context tokens
- provider-reported model token usage, when available

Do not present approximate context size as authoritative billing
information.

### Context Materialization

WayFinder can construct a deterministic, bounded repository context
package for a development task without making a model call.

The context compiler follows this conceptual pipeline:

```text
Task
→ Discover
→ Rank
→ Budget
→ Materialize
→ ContextPackage
```

The token budget is a ceiling, not a target. The compiler should return
the smallest useful package it can identify within the requested budget
rather than attempting to fill the budget.

Context may contain complete files or bounded excerpts. Selection
diagnostics may exist internally, but AI-facing MCP responses should
expose only the materialized context required by the consumer.

The intended AI exploration pattern is:

```text
context
→ targeted repo_search when more evidence is needed
→ repo_read for specific files
```

Do not respond to every insufficient context package by automatically
increasing the context budget. Prefer targeted follow-up exploration
when the missing information can be identified.

The MCP adapter currently uses a default context budget of 4,000 estimated
tokens. This is adapter policy rather than a `ContextCompiler` invariant;
callers may explicitly request a different positive budget. Copilot CLI
dogfooding compared 8,000, 4,000, and 2,000 token budgets on the same
repository investigation. The 8,000-token response was externalized by the
client as too large to read at once, while 4,000 remained directly consumable
and still provided useful orientation. The 2,000-token run also succeeded but
required more follow-up exploration. Treat 4,000 as a practical MCP default
observed through dogfooding, not as a provider-neutral optimum.

---

## Repository Intelligence (Milestone 17)

Repository Intelligence is implemented as shared Application/Infrastructure
services exposed by thin CLI and MCP adapters. It is read-only and intended
to locate relevant code before requesting source text.

- `repo_discover` identifies `.sln`, `.slnx`, and .NET project files;
  discovery caps are 100 solutions and 500 projects. These caps are
  enforced, but discovery truncation is not currently reported to callers.
- `repo_relationships` inspects target frameworks, project references, and
  external assembly reference metadata (`HintPath`, `CopyLocal`). It must not
  load or inspect referenced external DLL contents as part of this operation.
- `repo_symbols` uses Roslyn to discover C# namespaces, types, and members;
  return qualified names, project-relative paths, line locations, and
  truncation state where supported.
- `repo_source_lines` retrieves bounded source ranges;
  `repo_source_declaration` navigates to a declaration by qualified name.
  Neither tool grants unrestricted file access.
- Human-facing equivalents are `/repo discover`, `/repo relationships`,
  `/repo symbols`, `/repo source lines`, and `/repo source declaration`.
- All AI-facing operations must resolve registered projects and access
  project content through `IProjectFileSystem`. Reject absolute paths,
  traversal, and filesystem links escaping the registered project.
- Keep results deterministic, bounded, and project-relative; exclude
  configured generated/build directories and directory symlink traversal.
- Do not equate filesystem containment checks with a complete OS sandbox;
  symlink-chain and filesystem race conditions remain review considerations.
- For protocol regression coverage, launch the actual MCP stdio server,
  initialize an MCP client, call `tools/list`, and assert the expected tools.
  Keep diagnostic output on stderr, never protocol stdout.
- Roslyn is an approved dependency for C# symbol discovery. Preserve
  technology-agnostic inspection for non-.NET repositories and support
  inspecting legacy .NET Framework project metadata without requiring builds.

---

## Repository Search

Do not build a large semantic/vector index until there is a demonstrated
need.

The current repository search is deterministic, bounded,
project-relative, and text-only.

Current V1 behavior:

- ordinal case-insensitive literal text matching;
- maximum 50 returned matches;
- maximum 300 characters per returned line;
- files larger than 1 MiB are skipped;
- binary/NUL-containing and malformed UTF-8 files are skipped;
- results are ordered deterministically by path and line;
- `Truncated` indicates that additional matches existed beyond the
  returned result budget.

The MCP `repo_search` query is a single literal search string. Do not
describe or treat it as a regular expression, glob, or multi-term OR
expression.

Repository search must respect project boundaries and output budgets.

Ignored generated/build directories currently include:

```text
.git
.idea
.vs
.vscode
bin
obj
node_modules
dist
coverage
```

Rust/Tauri `target` directories have been observed in real search
results and are a candidate for the ignored-directory policy, but this
has not yet been adopted as policy.

Do not implement a custom `.gitignore` parser unless there is a strong
reason to do so.

---

## Git Integration

WayFinder provides read-only Git repository inspection through MCP.

### Available MCP Tools

- `git_status`: Returns the repository's working tree status.
- `git_diff`: Returns staged or unstaged changes, optionally restricted
  to a specific path. Supports stat-only output and truncation.
- `git_log`: Returns recent commit history, optionally filtered by path.

All tools operate exclusively on registered repositories.

### Implementation

The Git implementation resides in:

`src/WayFinder.DevTools.Infrastructure/Repositories/Git/`

`GitCommandRunner` is responsible for executing Git subprocesses.

### Security Requirements

Future changes MUST preserve the following constraints:

1. Git operations must remain read-only.
2. Never execute arbitrary Git commands supplied by an AI agent.
3. Never execute Git commands through a shell.
4. Use `ProcessStartInfo.ArgumentList` for argument handling.
5. Validate repository paths before executing Git.
6. Validate user-supplied file paths against the repository boundary.
7. Use literal Git pathspecs to prevent pathspec injection.
8. Remove inherited `GIT_*` environment variables.
9. Disable repository-configured filesystem monitors.
10. Disable external diff tools and text conversion for diff operations.
11. Enforce execution timeouts and bounded output.
12. Terminate subprocesses when output limits are exceeded.
13. Preserve strict UTF-8 decoding.

### Resource Limits

| Operation                  | stdout limit | stderr limit |
| -------------------------- | -----------: | -----------: |
| Git branch / commit checks |        4 KiB |       16 KiB |
| Git status                 |      256 KiB |       16 KiB |
| Git log                    |       64 KiB |       16 KiB |
| Git diff                   | Configurable |       16 KiB |

All Git subprocesses have a 15-second execution timeout.

Oversized status, log, and command output is rejected.

Oversized diff output is truncated and reported as truncated.

### Security Limitations

Read-only Git commands are not necessarily free of side effects.

Git status may invoke repository-configured clean filters.

Therefore:

- Only explicitly registered and trusted repositories may be inspected.
- Do not treat Git repository configuration as inherently safe.
- Do not describe the Git integration as a complete sandbox.
- Do not expand Git command capabilities without reviewing
  potential subprocess execution and filesystem side effects.

### Testing

Git integration changes must preserve existing regression coverage.

Run:

```bash
dotnet test
git diff --check
```

Tests should cover:

- Repository and path validation.
- Git argument and pathspec injection.
- Git environment isolation.
- Output limits and UTF-8 handling.
- Process termination and timeouts.
- MCP tool behavior and error handling.

### Development Guidelines

- Prefer small, incremental changes.
- Run tests after each meaningful change.
- Avoid introducing unnecessary abstractions.
- Do not refactor working Git infrastructure without a concrete reason.
- Preserve the existing MCP tool contracts.
- Keep MCP responses compact to minimize token consumption.

## AI Working Style

WayFinder is intended to improve collaboration between the developer and
AI, not eliminate that collaboration.

The desired development workflow is:

```text
inspect
→ discuss
→ decide
→ implement
→ test
→ review
→ discuss next milestone
```

AI agents may autonomously perform mechanical tasks when explicitly
permitted, such as:

- locating files
- reading bounded source sections
- finding references
- running tests
- running builds
- inspecting Git status
- inspecting Git diffs
- validating changes

AI agents should not silently make product or architecture decisions.

Examples requiring discussion include:

- architecture changes
- security model changes
- public API design
- UX decisions
- major dependency choices
- changing project semantics
- choosing the next major milestone

When implementation reveals a design decision, surface the decision
instead of silently choosing a direction.

Dogfooding is used to discover real integration problems, but do not
generalize from a single model behavior or one repository-specific
example without a durable invariant or repeated evidence.

Use this rule:

```text
dogfood to discover problems
→ generalize only when the problem represents a real invariant or repeats
```

Avoid tuning core architecture around quirks of a particular model,
provider, or single experiment.

---

## Target Development Domains (guidance, not yet new tools)

WayFinder's core should remain technology-agnostic. Copilot-specific
instructions and future specialized MCP tools should address the actual
C#/.NET/SQL Server development environments without assuming all projects
use the same framework, version control system, or host OS.

### C# and .NET

- Detect the target framework(s), SDK/toolchain and project format before
  recommending language features or NuGet upgrades.
- **Do not equate WayFinder's .NET 10 implementation baseline with customer
  project targets.** Legacy .NET Framework 4.8 and old-style project files
  must be investigated without requiring a migration, restore, or build.
- Distinguish SDK-style and legacy project reference handling; consider
  assembly binding redirects, framework references, 32-/64-bit dependencies,
  COM/interop, and Windows-only components where relevant.
- Do not assume source inspection can load all third-party assemblies, run
  MSBuild targets safely, or inspect locally installed Sage assemblies.
- Prefer Roslyn symbol navigation, precise line reads and project relationship
  metadata to dumping large source files.
- Prefer unit tests and minimal scoped changes; only run builds or tests if
  the relevant execution capability is explicitly authorized.

### Microsoft SQL Server (future MCP area)

- Separate environment registration from repository registration. A
  registered project must not implicitly expose a database or customer VM.
- Start with **metadata-only operations** (schemas, tables, views, columns,
  keys, dependencies and object definitions) on explicit allowlisted database
  targets, with read-only/least-privilege credentials.
- Do not provide a generic `execute_sql` or stored-procedure invoker as the
  first feature. Nominally read-only SQL statements can still consume
  resources, expose sensitive rows, or invoke functions with effects.
- Bound row/metadata sizes, timeouts, and concurrency; never return
  credentials or unredacted customer data by default.
- Treat actual query-plan or performance analysis as separately scoped work,
  with clear requirements for permissions and production safety.
- Never execute DDL/DML, maintenance, migration, or permission changes under
  an inspection capability.

### Sage 100 / AppDesigner and Windows client environments

- Respect customer-specific Windows VMs, installed Sage versions, external
  DLLs, and AppDesigner integration and deployment constraints.
- Do not assume Sage runtime/application APIs are available on the macOS
  WayFinder development host.
- Distinguish repository facts from knowledge of installed customer systems;
  never fabricate Sage object models, ERP tables or API availability.
- Treat solution deployment to customer VMs as a distinct, explicitly
  approved operation rather than a side effect of a code-analysis request.

### Version control

- Preserve the existing read-only Git implementation and trusted-repository
  caveats. Do not assume every client uses Git.
- **TFVC/TFS support is a candidate, not implemented.** Before adding it,
  define read-only use cases, authentication boundaries and safe tooling for
  Windows development environments.

---

## Write Operations

Initial AI-facing capabilities should be read-only wherever practical.

Future write operations must be explicit and controlled.

Do not assume that because a project is registered it is writable.

Read and write permissions should remain conceptually separate.

Potentially destructive operations require stronger safeguards than
inspection operations.

---

## Testing

Security boundaries require regression tests.

Tests should cover both expected behavior and attempted boundary
violations.

Examples include:

```text
normal project-relative path          allowed
../ traversal                         rejected
absolute path                         rejected
file symlink escaping project         rejected
directory symlink traversal           rejected
ignored build directory traversal     skipped
```

When fixing a security-boundary bug, add a regression test before
considering the issue resolved.

Run:

```text
dotnet test
dotnet build
```

before considering an implementation milestone complete.

Warnings are treated as errors.

---

## C# Style

Follow `.editorconfig`.

Prefer clear, idiomatic modern C#.

Use file-scoped namespaces.

Use `var` when the type is apparent.

Keep short expressions and constructor/method calls on one line when
they fit comfortably.

Preferred:

```text
var battery = new Battery(capacity: 100, charge: 75);
```

Avoid unnecessary multiline formatting when the expression remains
readable within the configured line length.

Favor small focused types and explicit contracts over large utility
classes.

Do not introduce abstractions solely in anticipation of hypothetical
future requirements.

---

## Dependency Policy

Keep external dependencies deliberate and minimal.

Before adding a package, consider whether:

- the .NET platform already provides the capability
- the package materially reduces complexity
- the package is maintained
- it works cross-platform
- its functionality belongs in WayFinder's trusted security boundary

Do not add dependencies merely to avoid writing trivial code.

Conversely, do not implement complex standards such as `.gitignore`
parsing from scratch when a mature solution is preferable.

---

## Current Direction

WayFinder development follows the Copilot-first, MCP-focused priorities and
milestones documented in `ROADMAP.md`. New interactive CLI features are frozen.
Do not treat a draft milestone or documentation update as implemented code.

Before beginning a new milestone:

1. Review the current roadmap.
2. Inspect the existing implementation.
3. Identify the smallest useful development increment.
4. Discuss architectural or security implications.
5. Implement and test incrementally.
6. Update documentation after completing the milestone.

Do not silently advance to new capabilities without discussing
their scope and security implications.

Preserve the distinction between:

- Read-only project inspection.
- Repository intelligence.
- Controlled execution.
- Project-specific intelligence.

Future execution or mutation capabilities require explicit
authorization and must not inherit permissions from project
registration.

---

## Design Principle

When choosing between convenience and a stronger AI-facing boundary,
prefer the stronger boundary unless the additional capability has a
concrete use case.

WayFinder should make safe, efficient behavior the easiest behavior to
implement.
