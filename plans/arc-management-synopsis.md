# ARC management stack: synopsis



This page is a short overview to support discussion. The detailed contracts and
verification records are in [the management plan](arc-management.md),
[the immutable redesign](mutation_safety_immutable.md),
[the Layer 1 plan](arc-base-model-layer-1.md) and
[the PolyglotSQLite plan](polyglot-sqlite.md).

## Projects and how they connect


| Project | Public namespaces and main types | Depends on | Native packages (npm / Python) |
|---|---|---|---|
| `src/ARCBaseModel` | `ARCBaseModel`: 13 entity classes, `EntityObject`, extension values (`Entity`, `EntityCollection`, `EntityNull`, `EntityBlob`), alternatives (`EntityReference`, `AnnotationValue`, `RecipeIntendedUse`, `DefinedTermSetReference`) | Fable.Core | `arc-base-model` / `arc_base_model` |
| `src/PolyglotSQLite` | `PolyglotSQLite`: `Sqlite`, `SqliteConnection`, `SqliteTransaction`, `SqlValue`, `SqlParameter`, `SqlRow`, `Table<'T>`, `TableRepository<'T>` | Microsoft.Data.Sqlite (.NET), better-sqlite3 (Node), stdlib `sqlite3` (Python) | `polyglot-sqlite` / `polyglot_sqlite` |
| `src/ARCManagement` | `ARCManagement`: `Session`, `ArcInfo`, `ArcHandle`, one operation group per entity type, `HistoryOperations`, `YAMLController`. `ARCtrl`: `ARC` | ARCBaseModel, PolyglotSQLite, YAMLicious | `arc-management` and `arc-management/arctrl` / `arc_management` and `arc_management.arctrl` |


All three libraries target `netstandard2.0` and transpile with Fable to
JavaScript/TypeScript and Python, including native consumer layers (`__init__.py`, `index.ts`). 

Each has a Pyxpecto suite that runs on all
three runtimes (`tests/<Project>.Tests`), and hand-written native consumer tests in JS, TS and Python
(`tests/<Project>.Native`). The JS and Python packages
share class definitions, so a `Dataset` from `arc-base-model` is the same class that
`arc-management` returns.

## Core requirements

#### Base Model:

1. One domain model for .NET, JS/TS and Python implements the shared entities and
   the three base profiles in [docs/spec](../docs/spec/index.md) (process provenance,
   semantic designation, administrative). A single `Dataset` class covers all three.
2. Values are immutable. Properties are getter-only, collections are frozen, and
   edits go through static `with<Property>` copies.
3. IDs are optional. The model never generates or infers them. Order, duplicates and
   shared object references are preserved.
4. Native callers pass plain values. Spec alternatives are erased unions, and no
   Fable runtime helpers are needed.
5. All methods besides getters are static.

#### Management:

1. The database connection is authoritative over the data model. Model values are
   snapshots of the SQLite state, which lives either in memory or in a file, with the
   same behaviour in both.
2. One database holds many ARCs. Domain IDs are scoped to one ARC.
3. Each ARC has one root Dataset, the hierarchical graph, and can additionally hold
   standalone objects that the root does not reach.
4. Edits take base-model values, write the database and return new immutable values.
   Supplied objects are never modified, and IDs are assigned only to the stored
   copies.
5. Each write is one transaction. A no-op or a failure leaves revision, history and
   data unchanged.
6. Undo and redo are linear, stored per ARC in SQLite, and survive closing,
    reopening and switching runtimes.
7. Reads pick up commits from other connections automatically. Returned values never
    change afterwards.
8. A detached YAML codec works without SQL or IO. It can encode and decode the base model and closely matches its shape.
9. Database files are interchangeable between .NET, Node and Python.
10. The management API is synchronous. It can be wrapped in async facades for JS and Python.
11. The management API always takes concrete paths for concrete I/O operations.

#### ARCtrl Workspace:

1. `ARCtrl.ARC` binds an ARC to a folder (called workspace). The persistent workspace storage is authoritative over the database. The SQLite database is a cache of the workspace state.
2. Workspace shape details are defined in the Workspace file, which can contain common profiles.
3. If no workspace file exists, the base case of a single `arc.yml` file is used.
4. SQLite database files are stored in a hidden `.arc` folder
5. Loading and saving is handled through a top-level API, with the workspace file handling running underneath.
6. Loading decides between the SQLite state and the workspace state. If both are changed, the workspace state is preferred. In this case the SQLite state is saved to a backup snapshot file.


## Immutable base model (Layer 1)

Implements one generic `EntityObject` class which stores `Type`, `AdditionalTypes`, `Id` and a dictionary of semi-dynamic properties. The properties themselves can be primitive values, nested `EntityObject`s, collections of either, or null. The model is immutable: every edit returns a new copy. 

The 13 entity classes are `Dataset`, `Process`, `Recipe`, `FormalParameter`,
`Sample`, `Data`, `Annotation`, `DefinedTerm`, `DefinedTermSet`, `Descriptor`,
`Agent`, `Organization` and `ScholarlyArticle`, which are all subclasses of `EntityObject`. Constructors take the required
spec fields, and optional fields are optional arguments. The model has no graph
indexes, back-links, codecs, persistence or ID policy. All of that belongs to
management.

```fsharp
open ARCBaseModel

let sample = Sample("leaf extract")
let temperature = Annotation("temperature", AnnotationValue.Number 21.5, unit = "degC")
let extraction = Process("extraction", input = EntityReference.Sample sample)

// Every edit is a copy. `extraction` itself never changes.
let withParams = Process.withParameterValues(extraction, [temperature])
let study = Dataset(["study-1"], conformsTo = ["process-provenance"], title = "Study 1", processes = [withParams])

// Generic copies on EntityObject return the concrete class.
let noted: Dataset = EntityObject.withProperty(study, "x-note", Entity.Text "draft")
let cleared = Dataset.withTitle(noted, None)   // an option clears
```

The same model from native code (JavaScript, then Python):

```javascript
import { Annotation, Process, Sample } from "arc-base-model";
const p = Process.withInput(new Process("extraction"), new Sample("leaf extract")); // erased union: pass the Sample itself
p.Name = "x";                                        // TypeError: getter-only
p.ParameterValues.push(new Annotation("temperature")); // TypeError: frozen array
```

```python
from arc_base_model import Process, Sample
p = Process.with_input(Process("extraction"), Sample("leaf extract"))
```

## PolyglotSQLite

PolyglotSQLite is a small synchronous SQLite API with the same behaviour on all three
runtimes, built on each platform's standard provider. It keeps the five SQLite
storage classes and exact int64 values, uses savepoints for nested transactions and
never commits a surrounding transaction implicitly. ARCManagement does all of its
database access through it. See the [PolyglotSQLite guide](../docs/project/polyglot-sqlite.md).

## ARCManagement session (`ARCManagement.Session`)

A `Session` owns one SQLite connection. It creates, opens, imports and lists ARC
entries. Each open entry is an `ArcHandle`.

### In-memory or on-disk

```fsharp
open ARCBaseModel
open ARCManagement

let scratch = Session.createInMemory()                // lasts as long as the connection
let session = Session.createFile("collection.sqlite") // new file; an existing file is rejected
let session' = Session.openFile("collection.sqlite")  // opens an existing file, or throws if it is not a valid ARC database
```

### One database, multiple ARC graphs

```fsharp
let arc1 = session.createArc(Dataset(["study-1"], conformsTo = ["process-provenance"])) // takes a root Dataset, returns an ArcHandle
let arc2 = session.importYamlString(yamlText)    // detached YAML becomes a new entry

for info in session.listArcs() do           // metadata only, no graph is loaded
    printfn "%s root=%s folder=%A" info.ArcId info.RootDatasetId info.Folder

let same = session.openArc(arc1.ArcId)     // returns the same ArcHandle while it is open
session.close()                             // closes every handle of this session
```

The `ArcId` is generated by the repository and is independent of the root Dataset's
ID. Two ARCs in one database can contain equal domain IDs without sharing anything.

### One root Dataset plus standalone objects

```fsharp
let root = arc1.RootDataset                          // fresh immutable Dataset read from SQL
let sample = arc1.SampleOperations.create("sample-1")  // stored, but not reachable from the root yet
arc1.HasStandaloneObjects                              // true: kept in SQLite, not exported to YAML
```

`RootDataset` is always the hierarchical graph. Objects created
through the operation groups live in the same ARC entry. They join the graph when a
relationship links them, and otherwise remain standalone SQLite-only objects.

### Editing: values in, new values out, database updated

Each `ArcHandle` has one operation group per entity type (`handle.DatasetOperations`,
`handle.ProcessOperations`, `handle.SampleOperations`, ...) plus `handle.EntityOperations` for generic typed objects
and extension properties. Every group has the same shape:

| Members | Meaning |
|---|---|
| Full Object Operations | |
| `create(args)` | Construct and store a new entity, returning the stored copy. Creates a new id when no id is provided. |
| `add(value)` | Strict insert of a value and its new nested entities. An existing ID fails |
| `upsert(value, ?recursive)` | Insert or replace the root's own fields. Nested stored entities are ignored links unless `recursive = true` |
| `get(id)`, `list()`, `delete(value)` | Read and delete. The root cannot be deleted, and Dataset deletion cascades only to exclusively owned children |
| Sub Property Operations||
| `set<Property>(value, replacement)` | One setter per property. An option clears it |
| `add<Property>` / `remove<Property>(value, target)` | One pair per entity collection |
| `get<Property>(value)` | One getter per property. Returns the current stored value, which may differ from the supplied snapshot |

```fsharp
let proc0 = arc1.ProcessOperations.create("extraction")
let proc1 = arc1.ProcessOperations.setInput(proc0, Some(EntityReference.Sample sample))  // returns the new value
let root1 = arc1.DatasetOperations.addProcess(arc1.RootDataset, proc1)                          // links proc1 into the root graph
let root2 = arc1.DatasetOperations.setTitle(root1, Some "Study 1")
// proc0 and root1 are unchanged snapshots. arc1.RootDataset reflects the latest commit.

let stored = arc1.SampleOperations.add(Sample("sample-2"))              // the stored copy carries an assigned ID
arc1.SampleOperations.upsert(Sample.withName(stored, "sample-2b")) |> ignore
arc1.DatasetOperations.upsert(editedTree, recursive = true) |> ignore   // every reachable entity is written as given
```

The subject of a setter is identified by its `Id`, and the setter applies to the
current stored state, so passing an older snapshot is fine. Relationship targets must
already be stored in the same ARC.

### Transactions

Every operation is one outermost SQLite transaction. It bumps the ARC revision,
reloads the state if another connection committed in the meantime, applies a pure
state transform, validates it, writes only the changed rows plus one history row and
commits.

```fsharp
arc1.SampleOperations.setName(sample, "sample-1") |> ignore   // unchanged result: rolled back, no revision or history entry
arc1.ProcessOperations.setInput(proc1, Some(EntityReference.Sample(Sample("x", id = "unknown"))))
// throws because the target is not stored. Nothing is written.
```

There are no caller revision tokens or stale-reference errors. SQLite allows only one
writer at a time, so this is not collaborative editing.

### History across sessions

```fsharp
let arcId = arc1.ArcId
arc1.DatasetOperations.setTitle(arc1.RootDataset, Some "Draft 2") |> ignore
arc1.HistoryOperations.undo()
session.close()

// Later, possibly in Python or JavaScript on the same file:
let session2 = Session.openFile("collection.sqlite")
let handle = session2.openArc(arcId)
handle.HistoryOperations.CanRedo   // true
handle.HistoryOperations.redo()    // the title is "Draft 2" again
```

History is linear per ARC. A new edit after an undo discards the redo branch. Each
history row stores the before and after rows of the changed entities only.

The history survives closing and reopening the session, and it is shared between runtimes.

### YAML

```fsharp
let text = YAMLController.Dataset.toYamlString(root)           // inline mode, absent IDs stay absent
let indexed = YAMLController.Dataset.toYamlString(root, indexed = true) // Agents, Recipes and Annotations with IDs in top-level sections
let detached = YAMLController.Dataset.fromYamlString(text)       // plain Dataset, no IDs assigned, no SQL
let exported = arc1.toYamlString()                 // root graph only, every managed ID written
```

Equal `id` values in one document denote one entity, and `{"@id": ...}` encodes a reference to that entity.

## Workspace facade (`ARCtrl.ARC`)

`ARC` wraps one `ArcHandle` with a folder binding. It forwards `RootDataset`, the
operation groups and `HistoryOperations`, and adds saving, dirty tracking and recovery.

```fsharp
open ARCtrl

let arc = ARC.create("C:/data/my-arc", "my-arc") // creates the root Dataset from its identifier; owns .arc/arc.sqlite
arc.DatasetOperations.setTitle(arc.RootDataset, Some "My ARC") |> ignore
arc.IsDirty     // true
arc.save()      // writes arc.yml for the root graph. History and standalone objects stay in SQLite
arc.close()

let resumed = ARC.openFolder("C:/data/my-arc", "auto")  // "auto" | "sql" | "yml" picks the source
let bound = ARC.bind(handle, "C:/data/other-arc")         // binds an existing ArcHandle from a borrowed session
```


## Points for discussion

These follow from the current design and are not yet decided:

- **Standalone objects.** 
   - They belong to one ARC entry, not to the session. A session-wide store, for example for agents or terms shared between ARCs, does not exist. Equal IDs in two ARCs are independent entities.
   - Should they be persisted to YAML by the workspace writer
- **Grouped edits.** Every call is its own transaction and its own undo step. Apart
  from `upsert(..., recursive = true)` there is no way to group several edits into
  one transaction or one undo step.
- **History growth.** Probably good to have prune function?
- **Cross-ARC queries.** The scoped `process_edges` and `core_validation_errors`
  views exist in SQL, but there is no public query API across ARCs.
- **Filesystem staging and recovery guarantees when only some writes succeed.**
- **JS IO synchronicity.** Whether workspace IO stays synchronous or gets explicit async facades for JS and Python.
- **Browser support.** Should the in-memory management be runnable in the JS browser? This would
  require a different SQLite provider, and the management API would potentially need to be async.
- **Static operations.** Should the object manipulation API consist only of static methods? In the
  current design, the `ArcHandle` exposes the operation groups as instance members
  (`arc1.SampleOperations.create(...)`), while the base model uses statics
  (`Sample.withName(...)`). Static functions that take the handle would be consistent with the base
  model, and instance members are easier to discover.
