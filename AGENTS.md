# AGENTS.md

## Project Overview

WayFinder.DevTools is a local-first developer tooling platform written
in C#/.NET.

Its purpose is to provide safe, efficient, structured access to software
projects for:

-   Human developers through the `wayfinder` CLI.
-   AI clients such as ChatGPT and Claude through an MCP server.
-   Future project-specific developer tooling.

The CLI and MCP server are adapters over shared application services.
Business logic must not be duplicated between them.

WayFinder should remain provider-neutral. Core functionality must not
depend on a specific AI provider.

------------------------------------------------------------------------

## Development Environment

WayFinder is developed primarily on macOS and must remain cross-platform
unless a feature explicitly requires otherwise.

Current baseline:

-   .NET 10
-   C#
-   macOS on Apple Silicon
-   VS Code
-   xUnit

Avoid Windows-specific APIs, filesystem assumptions, path separators,
environment variables, or shell behavior unless isolated behind an
explicit platform abstraction.

Use `Path`, `DirectoryInfo`, and other cross-platform .NET APIs instead
of manually constructing filesystem paths.

------------------------------------------------------------------------

## Architecture

The solution is divided into these primary projects:

``` text
WayFinder.DevTools.Application
WayFinder.DevTools.Infrastructure
WayFinder.DevTools.Cli
WayFinder.DevTools.Mcp
```

The CLI and MCP projects are sibling adapters over the same Application
and Infrastructure services. Neither adapter should call the other.

Dependencies should flow toward the application layer.

Conceptually:

``` text
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

-   project context
-   project inspection
-   project detectors
-   project filesystem contracts
-   project registry and registered-project resolution contracts
-   repository search contracts
-   bounded repository source retrieval contracts
-   deterministic context compilation contracts

Application code should not depend directly on operating-system
filesystem or process APIs when an abstraction is appropriate.

### Infrastructure

Implements interaction with the host environment.

Examples:

-   filesystem access
-   Git
-   dotnet CLI
-   process execution
-   project discovery
-   project artifact detection

Infrastructure must enforce security boundaries required by the
Application contracts.

### CLI

The CLI is a human-facing adapter.

It should contain:

-   command definitions
-   argument parsing
-   human-readable presentation
-   composition/wiring

It should contain as little business logic as possible.

### MCP

The MCP server is an AI-facing adapter over the same Application and
Infrastructure services used by the CLI.

The current read-only MCP tool surface is:

``` text
projects
context
repo_search
repo_read
```

MCP tools must remain thin adapters. Validation and response mapping
that are specific to the protocol belong in MCP; repository behavior,
security policy, context selection, and other reusable business logic
belong in shared Application or Infrastructure services.

The MCP server uses stdio transport. Treat the streams as a protocol
boundary:

``` text
stdin   MCP protocol input
stdout  MCP protocol output
stderr  diagnostics and logging
```

Never write human-readable diagnostics to stdout from the MCP process.

------------------------------------------------------------------------

## Project Access Security

Project isolation is a core security invariant.

AI-facing operations may access only files contained within projects
explicitly made available to WayFinder.

The project filesystem boundary is represented by:

``` text
IProjectFileSystem
```

AI-facing project functionality must not bypass this abstraction with
direct calls to:

``` text
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

``` text
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

------------------------------------------------------------------------

## Project Registry Security

WayFinder will support two project-discovery models.

### CLI

The human CLI may discover the Git repository containing the current
working directory.

This allows commands such as:

``` text
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

------------------------------------------------------------------------

## Filesystem Access

Avoid creating a generic filesystem service exposed to AI clients.

Do not expose operations equivalent to:

``` text
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

Current V1 behavior:

-   maximum returned source content is 64 KiB;
-   total byte count and truncation state are reported;
-   NUL-containing files are treated as unsupported binary content;
-   malformed UTF-8 is rejected;
-   a UTF-8 BOM is accepted and removed;
-   when truncation cuts through a UTF-8 sequence, the incomplete
    sequence is omitted safely.

Large files must not be returned automatically. Do not expose an
arbitrary filesystem read primitive to work around repository read
limits.

------------------------------------------------------------------------

## Shell and Process Execution

Do not expose a generic MCP tool equivalent to:

``` text
shell(command)
```

or:

``` text
execute(command)
```

Instead expose constrained operations with explicit semantics.

Examples:

``` text
git_status
git_diff
dotnet_build
dotnet_test
repo_search
repo_read
```

Each operation should define:

-   allowed inputs
-   execution scope
-   timeout
-   output limits
-   structured result format

Prefer deterministic operations over arbitrary command execution.

------------------------------------------------------------------------

## Project Inspection

Project inspection is detector-based.

Each detector implements:

``` text
IProjectDetector
```

Detectors should identify factual project artifacts rather than making
unnecessary assumptions.

Examples:

``` text
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

``` text
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

------------------------------------------------------------------------

## Detection vs Interpretation

Keep these concepts separate:

``` text
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

------------------------------------------------------------------------

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

-   `technologies.json` describes technologies WayFinder knows how to
    recognize.
-   `wayfinder.json` describes technologies a particular project
    declares that it uses.
-   Detection represents repository evidence.
-   Declaration represents developer-provided project knowledge.
-   AI consumers may understand technologies that WayFinder itself does
    not recognize.

WayFinder must remain useful for projects that do not use .NET, Node,
DeadRoute-specific conventions, or any other particular ecosystem.

## Project Manifest

A repository may contain a root-level `wayfinder.json`.

The manifest is optional for normal CLI inspection but will provide
explicit project metadata and may later participate in AI authorization
and context generation.

Manifest version 1:

-   `version` is required and must be `1`.
-   `name` is optional.
-   `technologies` is required and may be empty.
-   Technology identifiers must be non-empty.
-   Duplicate technology identifiers are invalid.
-   Unknown technology identifiers are valid.
-   Extra JSON properties should remain tolerated for forward
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

-   inspected;
-   searched;
-   read through bounded WayFinder read operations;
-   used to construct project context.

Registration alone must never permit:

-   creating files;
-   modifying files;
-   deleting files;
-   executing arbitrary commands;
-   changing Git state;
-   committing or pushing;
-   installing dependencies;
-   running arbitrary scripts.

Any future AI-facing mutation capability must use a separate explicit
authorization model. Write authorization must never be inferred from
project registration.

All AI-facing filesystem operations must continue to pass through
WayFinder's project filesystem security boundary.

The project registry must not provide a mechanism for escaping
project-root containment.

AI-facing tools must resolve project access through the
registered-project resolver before invoking repository operations. MCP
callers currently address registered projects by ID; arbitrary
filesystem roots are never accepted as project selectors.

## CLI Output

Human-facing CLI output may be formatted for readability.

Generic inspection output should remain capable of representing
detectors unknown to the CLI.

Avoid hardcoding rendering logic such as:

``` text
WriteDotNetSection
WriteNodeSection
WriteRustSection
```

when generic artifact rendering is sufficient.

Machine-facing MCP output should prefer structured data over prose.

------------------------------------------------------------------------

## Determinism

WayFinder should produce deterministic results wherever practical.

Stable ordering is preferred for:

-   files
-   search results
-   project artifacts
-   Git output
-   structured MCP responses

Deterministic output improves:

-   reproducibility
-   testing
-   diff quality
-   caching
-   AI context stability
-   token efficiency

Do not depend on filesystem enumeration order.

------------------------------------------------------------------------

## Token Efficiency

MCP itself does not make AI usage token-efficient.

WayFinder must actively control the amount of context returned to AI
clients.

Repository operations should support bounded output.

Examples include:

-   maximum result count
-   maximum lines
-   maximum bytes
-   targeted file ranges
-   search snippets
-   truncation metadata
-   continuation mechanisms

Prefer:

``` text
small structured result
```

over:

``` text
large repository dump
```

Never send an entire repository to an AI model as a default context
strategy.

Future telemetry should distinguish between:

-   operation duration
-   output bytes
-   approximate emitted context tokens
-   provider-reported model token usage, when available

Do not present approximate context size as authoritative billing
information.

### Context Materialization

WayFinder can construct a deterministic, bounded repository context
package for a development task without making a model call.

The context compiler follows this conceptual pipeline:

``` text
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

``` text
context
→ targeted repo_search when more evidence is needed
→ repo_read for specific files
```

Do not respond to every insufficient context package by automatically
increasing the context budget. Prefer targeted follow-up exploration
when the missing information can be identified.

------------------------------------------------------------------------

## Repository Search

Do not build a large semantic/vector index until there is a demonstrated
need.

The current repository search is deterministic, bounded,
project-relative, and text-only.

Current V1 behavior:

-   ordinal case-insensitive literal text matching;
-   maximum 50 returned matches;
-   maximum 300 characters per returned line;
-   files larger than 1 MiB are skipped;
-   binary/NUL-containing and malformed UTF-8 files are skipped;
-   results are ordered deterministically by path and line;
-   `Truncated` indicates that additional matches existed beyond the
    returned result budget.

The MCP `repo_search` query is a single literal search string. Do not
describe or treat it as a regular expression, glob, or multi-term OR
expression.

Repository search must respect project boundaries and output budgets.

Ignored generated/build directories currently include:

``` text
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

------------------------------------------------------------------------

## AI Working Style

WayFinder is intended to improve collaboration between the developer and
AI, not eliminate that collaboration.

The desired development workflow is:

``` text
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

-   locating files
-   reading bounded source sections
-   finding references
-   running tests
-   running builds
-   inspecting Git status
-   inspecting Git diffs
-   validating changes

AI agents should not silently make product or architecture decisions.

Examples requiring discussion include:

-   architecture changes
-   security model changes
-   public API design
-   UX decisions
-   major dependency choices
-   changing project semantics
-   choosing the next major milestone

When implementation reveals a design decision, surface the decision
instead of silently choosing a direction.

Dogfooding is used to discover real integration problems, but do not
generalize from a single model behavior or one repository-specific
example without a durable invariant or repeated evidence.

Use this rule:

``` text
dogfood to discover problems
→ generalize only when the problem represents a real invariant or repeats
```

Avoid tuning core architecture around quirks of a particular model,
provider, or single experiment.

------------------------------------------------------------------------

## Write Operations

Initial AI-facing capabilities should be read-only wherever practical.

Future write operations must be explicit and controlled.

Do not assume that because a project is registered it is writable.

Read and write permissions should remain conceptually separate.

Potentially destructive operations require stronger safeguards than
inspection operations.

------------------------------------------------------------------------

## Testing

Security boundaries require regression tests.

Tests should cover both expected behavior and attempted boundary
violations.

Examples include:

``` text
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

``` text
dotnet test
dotnet build
```

before considering an implementation milestone complete.

Warnings are treated as errors.

------------------------------------------------------------------------

## C# Style

Follow `.editorconfig`.

Prefer clear, idiomatic modern C#.

Use file-scoped namespaces.

Use `var` when the type is apparent.

Keep short expressions and constructor/method calls on one line when
they fit comfortably.

Preferred:

``` text
var battery = new Battery(capacity: 100, charge: 75);
```

Avoid unnecessary multiline formatting when the expression remains
readable within the configured line length.

Favor small focused types and explicit contracts over large utility
classes.

Do not introduce abstractions solely in anticipation of hypothetical
future requirements.

------------------------------------------------------------------------

## Dependency Policy

Keep external dependencies deliberate and minimal.

Before adding a package, consider whether:

-   the .NET platform already provides the capability
-   the package materially reduces complexity
-   the package is maintained
-   it works cross-platform
-   its functionality belongs in WayFinder's trusted security boundary

Do not add dependencies merely to avoid writing trivial code.

Conversely, do not implement complex standards such as `.gitignore`
parsing from scratch when a mature solution is preferable.

------------------------------------------------------------------------

## Current Direction

The implementation sequence remains:

``` text
1. Foundation and project discovery
2. Safe project filesystem boundary
3. Project intelligence and detection
4. Project registry
5. Repository intelligence
6. MCP adapter
7. Controlled execution capabilities
8. Project-specific intelligence
```

Stages 1 through 6 now have working implementations. In particular, the
MCP adapter has been exercised through a real external MCP client using
the `projects`, `context`, `repo_search`, and `repo_read` tools.

The current MCP surface remains read-only. Do not silently advance to
controlled execution or project-specific intelligence merely because
they are later in the sequence.

The remaining open context-policy question is whether the MCP adapter
should use a smaller default context budget than the general Application
default. This has not been decided.

### Next Milestone: AI Project References

The next implementation milestone is to improve AI-facing project
addressing.

MCP tools that operate on a registered project should accept either:

-   the registered project's authoritative GUID; or
-   its exact registered name.

Resolution belongs in `IRegisteredProjectResolver`.

Resolution rules:

1.  If the supplied value parses as a GUID, resolve it as a registered
    project ID.
2.  Otherwise, match it against registered project names.
3.  Zero matches is a not-found error.
4.  Exactly one match resolves the project.
5.  Multiple matches are ambiguous and must fail with an error directing
    the caller to use the project ID.

Duplicate project names remain valid. Names are convenient references,
not identities. WayFinder must never select an arbitrary project when a
name is ambiguous.

Do not add fuzzy matching, partial matching, aliases, path-based
resolution, or filesystem discovery as part of this milestone.

Keep the strongly typed `Resolve(Guid)` operation and add
string-reference resolution to `IRegisteredProjectResolver`. Do not add
name lookup to `IProjectRegistry` unless a demonstrated need appears. Do
not introduce a new project-reference abstraction solely for this
milestone.

Apply the resulting `project` parameter consistently to:

-   `context`
-   `repo_search`
-   `repo_read`

Add resolver regression tests covering:

-   resolution by `Guid`;
-   resolution by GUID string;
-   resolution by unique exact registered name;
-   unknown names;
-   empty references;
-   partial-name mismatches;
-   duplicate-name ambiguity.

Name matching should remain exact. Case-sensitivity is the one remaining
low-level semantic detail to confirm before implementation; do not
silently choose a different matching policy.

After implementation, verify the behavior through a real MCP client by
calling a repository tool directly with a unique registered project
name, for example `repo_search("DeadRoute", "stale")`, without first
resolving the project through `projects`.

The sequence may change after discussion.

------------------------------------------------------------------------

## Design Principle

When choosing between convenience and a stronger AI-facing boundary,
prefer the stronger boundary unless the additional capability has a
concrete use case.

WayFinder should make safe, efficient behavior the easiest behavior to
implement.
