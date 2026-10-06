# AGENTS.md

## Project Overview

WayFinder.DevTools is a local-first developer tooling platform written in C#/.NET.

Its purpose is to provide safe, efficient, structured access to software projects for:

- Human developers through the `wayfinder` CLI.
- AI clients such as ChatGPT and Claude through an MCP server.
- Future project-specific developer tooling.

The CLI and MCP server are adapters over shared application services. Business logic must not be duplicated between them.

WayFinder should remain provider-neutral. Core functionality must not depend on a specific AI provider.

---

## Development Environment

WayFinder is developed primarily on macOS and must remain cross-platform unless a feature explicitly requires otherwise.

Current baseline:

- .NET 10
- C#
- macOS on Apple Silicon
- VS Code
- xUnit

Avoid Windows-specific APIs, filesystem assumptions, path separators, environment variables, or shell behavior unless isolated behind an explicit platform abstraction.

Use `Path`, `DirectoryInfo`, and other cross-platform .NET APIs instead of manually constructing filesystem paths.

---

## Architecture

The solution is divided into these primary layers:

```text
WayFinder.DevTools.Application
WayFinder.DevTools.Infrastructure
WayFinder.DevTools.Cli
```

A future MCP adapter will be added separately:

```text
WayFinder.DevTools.Mcp
```

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
- future repository search contracts
- future bounded source retrieval contracts

Application code should not depend directly on operating-system filesystem or process APIs when an abstraction is appropriate.

### Infrastructure

Implements interaction with the host environment.

Examples:

- filesystem access
- Git
- dotnet CLI
- process execution
- project discovery
- project artifact detection

Infrastructure must enforce security boundaries required by the Application contracts.

### CLI

The CLI is a human-facing adapter.

It should contain:

- command definitions
- argument parsing
- human-readable presentation
- composition/wiring

It should contain as little business logic as possible.

### MCP

The future MCP server is an AI-facing adapter over the same Application services used by the CLI.

Do not implement functionality exclusively inside MCP when it belongs in shared application services.

---

## Project Access Security

Project isolation is a core security invariant.

AI-facing operations may access only files contained within projects explicitly made available to WayFinder.

The project filesystem boundary is represented by:

```text
IProjectFileSystem
```

AI-facing project functionality must not bypass this abstraction with direct calls to:

```text
File
Directory
FileInfo
DirectoryInfo
```

when accessing project contents.

Infrastructure implementations may use these APIs internally to implement the boundary.

### Path Rules

Project-facing paths should normally be project-relative.

Do not expose arbitrary absolute filesystem paths to AI-facing APIs.

Reject attempts to escape a project through:

```text
../
absolute paths
filesystem links resolving outside the project
```

Canonical containment must be verified before exposing filesystem content.

Directory symbolic links are not traversed by the V1 project filesystem implementation.

Default to denying additional filesystem capability unless there is a demonstrated need for it.

---

## Project Registry Security

WayFinder will support two project-discovery models.

### CLI

The human CLI may discover the Git repository containing the current working directory.

This allows commands such as:

```text
wayfinder project info
```

without requiring prior registration.

### MCP / AI Clients

AI clients should only receive access to projects explicitly registered with WayFinder.

An AI client must not be able to recursively discover arbitrary repositories or directories elsewhere on the user's computer.

Registering a project grants visibility to that project. It must not automatically imply unrestricted write access.

WayFinder's own internal configuration storage is separate from the project filesystem exposed to AI clients.

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

Future source reading should be bounded.

Prefer interfaces conceptually similar to:

```text
ReadText(
    project,
    relativePath,
    startLine,
    lineCount
)
```

rather than returning entire files without limits.

Large files must not be returned automatically.

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

Detectors should identify factual project artifacts rather than making unnecessary assumptions.

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

Adding a detector should not require modifying generic inspection or rendering logic.

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

Do not introduce large central technology enums or models that require WayFinder to know every possible ecosystem in advance.

---

## Technology Model

WayFinder is technology-agnostic and must not assume that its built-in
technology catalog represents the complete universe of technologies.

Technology identifiers are open-ended strings, not enums or other closed
type systems.

Built-in technology recognition is data-driven through the embedded
`technologies.json` catalog. Adding recognition for another ecosystem
should normally require adding or changing catalog data, not implementing
another technology-specific detector.

The generic `TechnologySignatureDetector` interprets technology signatures.
Do not create detectors such as `SwiftProjectDetector`,
`PythonProjectDetector`, or `RustProjectDetector` when the technology can be
described through signatures.

Special-purpose detectors remain appropriate when behavior cannot reasonably
be represented by technology signatures.

Project manifests may declare technologies that are unknown to WayFinder.
Unknown technology identifiers are valid project metadata and must not be
rejected.

Keep these concepts separate:

- `technologies.json` describes technologies WayFinder knows how to recognize.
- `wayfinder.json` describes technologies a particular project declares that
  it uses.
- Detection represents repository evidence.
- Declaration represents developer-provided project knowledge.
- AI consumers may understand technologies that WayFinder itself does not
  recognize.

WayFinder must remain useful for projects that do not use .NET, Node,
DeadRoute-specific conventions, or any other particular ecosystem.

## Project Manifest

A repository may contain a root-level `wayfinder.json`.

The manifest is optional for normal CLI inspection but will provide explicit
project metadata and may later participate in AI authorization and context
generation.

Manifest version 1:

- `version` is required and must be `1`.
- `name` is optional.
- `technologies` is required and may be empty.
- Technology identifiers must be non-empty.
- Duplicate technology identifiers are invalid.
- Unknown technology identifiers are valid.
- Extra JSON properties should remain tolerated for forward compatibility.

Reading project manifests must go through `IProjectFileSystem`. Do not bypass
the project filesystem security boundary with direct arbitrary file access.

Commands named `info`, `inspect`, `list`, `show`, or similar observational
operations must remain read-only. A future `project init` command may create
`wayfinder.json`, but initialization must be an explicit write operation.

## CLI Output

Human-facing CLI output may be formatted for readability.

Generic inspection output should remain capable of representing detectors unknown to the CLI.

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

WayFinder must actively control the amount of context returned to AI clients.

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

Never send an entire repository to an AI model as a default context strategy.

Future telemetry should distinguish between:

- operation duration
- output bytes
- approximate emitted context tokens
- provider-reported model token usage, when available

Do not present approximate context size as authoritative billing information.

---

## Repository Search

Do not build a large semantic/vector index until there is a demonstrated need.

Prefer deterministic repository search initially.

Potential implementations may use Git or ripgrep semantics.

Repository search must respect project boundaries and output budgets.

Ignore generated/build directories where appropriate, including common examples such as:

```text
.git
bin
obj
node_modules
dist
coverage
```

Do not implement a custom `.gitignore` parser unless there is a strong reason to do so.

---

## AI Working Style

WayFinder is intended to improve collaboration between the developer and AI, not eliminate that collaboration.

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

AI agents may autonomously perform mechanical tasks when explicitly permitted, such as:

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

When implementation reveals a design decision, surface the decision instead of silently choosing a direction.

---

## Write Operations

Initial AI-facing capabilities should be read-only wherever practical.

Future write operations must be explicit and controlled.

Do not assume that because a project is registered it is writable.

Read and write permissions should remain conceptually separate.

Potentially destructive operations require stronger safeguards than inspection operations.

---

## Testing

Security boundaries require regression tests.

Tests should cover both expected behavior and attempted boundary violations.

Examples include:

```text
normal project-relative path          allowed
../ traversal                         rejected
absolute path                         rejected
file symlink escaping project         rejected
directory symlink traversal           rejected
ignored build directory traversal     skipped
```

When fixing a security-boundary bug, add a regression test before considering the issue resolved.

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

Keep short expressions and constructor/method calls on one line when they fit comfortably.

Preferred:

```text
var battery = new Battery(capacity: 100, charge: 75);
```

Avoid unnecessary multiline formatting when the expression remains readable within the configured line length.

Favor small focused types and explicit contracts over large utility classes.

Do not introduce abstractions solely in anticipation of hypothetical future requirements.

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

Conversely, do not implement complex standards such as `.gitignore` parsing from scratch when a mature solution is preferable.

---

## Current Direction

The current implementation sequence is:

```text
1. Foundation and project discovery
2. Safe project filesystem boundary
3. Project intelligence and detection
4. Project registry
5. Repository intelligence
6. MCP adapter
7. Controlled execution capabilities
8. Project-specific intelligence
```

The sequence may change after discussion.

Do not silently advance to later stages merely because they are listed here.

---

## Design Principle

When choosing between convenience and a stronger AI-facing boundary, prefer the stronger boundary unless the additional capability has a concrete use case.

WayFinder should make safe, efficient behavior the easiest behavior to implement.
