# ARC Base Model

Specification and implementation workspace for the ARC data model, with three base profiles: Process Provenance, Semantic Designation, and Administrative.

The repository contains two portable F# libraries for .NET, JavaScript/TypeScript, and Python:

- [ARCBaseModel](docs/project/abstract-model.md): mutable domain classes representing the base specification.
- [PolyglotSQLite](docs/project/polyglot-sqlite.md): independent SQLite values, connections, transactions, and table repositories.

Builds stage local native packages; registry publication is outside the current implementation scope.

- [Project overview](docs/project/overview.md)
- [Normative specification](docs/spec/index.md)
- [Implementation and build guide](docs/project/implementation.md)
- [Examples and schemas](docs/project/examples-and-schemas.md)
- [Reference material](docs/project/references.md)

Restore the pinned tools and dependencies, then build and test:

```powershell
dotnet tool restore
npm ci
uv sync --frozen
.\build.cmd BuildSolution
npm run build
npm run build:py
.\build.cmd RunTests
.\build.cmd BuildDocs
```

`RunTests` verifies both libraries across .NET, JavaScript, and Python, including native consumers, TypeScript declarations, packed SQLite artifacts, and cross-runtime database interoperability.
