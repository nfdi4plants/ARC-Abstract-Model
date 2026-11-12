---
title: Implementation Guide
category: Project
categoryindex: 2
index: 3
---

# Implementation Guide

The repository implements the ARC Abstract Model specification and portable SQLite infrastructure as independent F# libraries. Both target .NET Standard 2.0 and use the pinned Fable toolchain for JavaScript/TypeScript and Python.

## Projects

| Project | Role | Runtime |
|---------|------|---------|
| `src/ARCBaseModel` | Layer 1 domain classes for all three Abstract Model profiles | .NET, JavaScript, Python |
| `src/PolyglotSQLite` | SQLite values, connections, transactions, and generic table repositories | .NET, Node, Python |
| `tests/ARCBaseModel.Tests` | Shared domain behavior and mapping probe | .NET, JavaScript, Python |
| `tests/ARCBaseModel.Native` | Handwritten JavaScript, TypeScript, and Python consumers | Node, Python |
| `tests/PolyglotSQLite.Tests` | Shared SQLite behavior and database interoperability | .NET, Node, Python |
| `tests/PolyglotSQLite.Native` | Handwritten consumers of staged and locally packed packages | Node, Python |

The [Layer 1 guide](abstract-model.md) documents construction, alternatives, native packages, and optional IDs. Graph indexes, traversal, normalization, codecs, and persistence belong to operational layers outside this library.

The [PolyglotSQLite guide](polyglot-sqlite.md) documents the SQLite API and ownership contract. Providers are Microsoft.Data.Sqlite for .NET, better-sqlite3 for Node, and stdlib sqlite3 for Python. ARC entity mappings, schemas, and ID policies remain outside this infrastructure library.

## SQL Profile Artifacts

The SQL profile remains a separate derived representation in `schemas/sql/`:

- `001_core.sql` contains executable SQLite DDL for the process graph profile.
- `seed_example.sql` contains a small seeded process graph.
- `seeded_core.sqlite` is the generated SQLite database.
- `design.md` explains the relational design.

These artifacts are not an implemented ARC persistence layer in either library.

## Build and Test

Restore dependencies with `dotnet tool restore`, `npm ci`, and `uv sync --frozen`, then use the root FAKE wrapper:

```powershell
.\build.cmd BuildSolution
.\build.cmd BuildBaseModel
.\build.cmd BuildPolyglotSQLite
.\build.cmd RunTests
.\build.cmd BuildDocs
```

`RunTests` builds the solution and runs both complete verification targets, `TestBaseModel` and `TestPolyglotSQLite`. They include shared tests across all three runtimes, handwritten native consumers, declaration checks, and the SQLite packed-package and database interoperability checks.

For the shared tests on a single runtime, use `RunTestsDotnet`, `RunTestsJS`, or `RunTestsPy`. `npm run test:js` invokes `RunTestsJS`. Full library checks remain available separately through `TestBaseModel` and `TestPolyglotSQLite`.

Builds stage local native artifacts under `build/out/base-model` and `build/out/polyglot-sqlite`. Both libraries remain non-packable as NuGet projects; registry publication is outside the current implementation scope.

## Shared Build Infrastructure

The build project retains its reusable process helpers, prerelease/version handling, prompts, package targets (`PackDotNet`, `PackJS`, `PackPy`, and `Pack`), registry publishing tasks, tag creation, and `Release` orchestration. Documentation build/watch targets use the release-note version. Registry publication remains separate from local artifact verification.

`TranspileTS` and `TranspilePy` run both libraries' supported native build pipelines. TypeScript output is bundled by the shared `vite.config.mts`; the staging scripts generate and check the curated declarations and public entrypoints. Python staging preserves the numeric compatibility boundary, precise stubs, and `py.typed` markers. Raw compiler output and staged packages stay under each library's `build/out/` subtree.

The root npm entrypoints use these same pipelines:

```powershell
npm run build
npm run build:base-model
npm run build:sqlite
npm run build:py
```

`npm run build` builds both JavaScript packages with TypeScript declarations; the two library scripts build one package each. `npm run build:py` stages both Python packages. The Vite library modes are selected by the staging scripts after Fable compilation, so the npm commands include the required preparation and declaration generation.

`PackJS` packs the two staged npm libraries into `dist/ts`; `PackPy` packs both staged Python libraries into `dist/py` using the existing standard-library wheel construction. These are local `0.0.0` verification artifacts. The native test targets exercise handwritten consumers and declarations against both staged and packed packages.
