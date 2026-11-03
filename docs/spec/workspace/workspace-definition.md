---
title: ARC Workspace Definition (Draft)
category: Specification
categoryindex: 3
index: 6
---

# ARC Workspace Definition

A workspace definition maps ARC model properties to files and folders. Applications
use it to read, write, and validate a workspace, and determine what is eligible
for publication. This is a design draft; the [model specifications](../index.md)
remain authoritative for entity properties and cardinalities.

See [LinkML workspace examples](linkml-examples.md) for an alternative using
LinkML model constraints and a custom layout annotation.

## Example

The YAML root describes the root Dataset. Ordinary keys are model properties;
their values describe storage. Keys prefixed with `$` are template instructions.

```yaml
type: metadata/type.yml
conformsTo: metadata/conformsTo.yml
identifiers: metadata/identifiers.yml
title: metadata/title.yml
agents: metadata/agents.yml
citations: metadata/citations.yml

dataFiles:
  - $directory: /data
    $when:
      type: Data
    $file: data.yml

hasParts:
  - metadata/hasParts.yml # Fallback for unmatched child Datasets.
  - $directory: /assays
    $when:
      type: Dataset
      additionalTypes:
        $contains: assay
    $each:
      $directory: "${item.identifiers[0]}"
      $file: dataset.yml
```

This stores root metadata under `/metadata`, Data records in `/data/data.yml`,
and one Dataset per `/assays/<identifier>/dataset.yml`. The `assay` classification
is illustrative. Other populated properties need bindings too.

## Bindings

A property string names the file containing its value:
`agents: metadata/agents.yml` is shorthand for
`agents: {$file: metadata/agents.yml}`. The file contains the value directly:
`type.yml` contains `Dataset`; `agents.yml` contains a sequence of Agent objects.
`.yml` and `.yaml` select YAML by default; other formats need a parser and writer.

| Instruction | Meaning |
|---|---|
| `$file` | Read/write the bound value. On a collection rule it stores a sequence; inside `$each` it stores one object. |
| `$directory` | Establish a directory scope, also used for payload placement in Data rules. |
| `$when` | Select objects by their model properties. Values here are literal conditions. |
| `$contains` | Test membership in a collection such as `additionalTypes`. |
| `$each` | Apply a nested template separately to each selected object; `item` refers to that object. |

Inside `$each`, ordinary property bindings can replace a whole-object `$file`,
allowing the same field-by-field layout used at the root.

Metadata and payloads are separate: `/data/data.yml` describes files whose
[Data.path](../shared/Data.md) values identify their contents. An extra file placed
in `/data` is not automatically registered. In this draft, persisted `Data.path`
values are workspace-relative, for example `data/measurements.csv`.

Missing required metadata, malformed sources, and populated properties without
bindings are errors. Missing optional metadata may represent absence. Supplied
values and duplicate occurrences must not be silently dropped or merged. IDs
remain optional and must not be inferred from folders or filenames.

## Selection and scope

- All `$when` conditions must match. Equality and `$contains` are case-sensitive;
  a missing property does not match. An omitted `$when` matches every member.
- Exactly one explicit rule may match. Multiple matches are errors. With no
  match, use the collection's single fallback: the plain file string in its rule
  sequence. Without a fallback, report a missing binding. Rule order is irrelevant.
- A fallback stores unmatched model objects; it does not discover arbitrary files.
  Invalid destinations and collisions are errors, not reasons to use a fallback.
- Rules apply to their containing property. Root `dataFiles` rules do not
  automatically apply inside child Datasets or processes.
- `/data` means the workspace root's `data` folder; `data` is relative to the
  current scope. `$directory` changes that scope. Reading a file does not.
  Resolved local paths must stay within the workspace.

The example binds `item` before evaluating its directory. Choosing
`identifiers[0]` is a layout policy: the value must be a valid, distinct folder
name. It is neither a canonical model identifier nor permission to rewrite one.
Each child retains its own `conformsTo` declarations.

## Workspace management and validation

Reading loads declared metadata sources. Inspection also searches for misplaced
or unmanaged content so applications can help users repair it. Recognizable
invalid objects must remain visible with their source locations. Expected paths
can be calculated only when the object's owning Dataset and collection are known.

| State | Meaning |
|---|---|
| Eligible for publication | Model values, placement, bindings, and required resources are valid. |
| Invalid | Intended model content has errors, including a missing binding or payload. |
| Unmanaged | Physical content has no established model binding or intended model role. |

Unmanaged content is not inherently erroneous. Applications may expose it through
a virtual **Unmanaged** folder without moving the physical files.

Validation reports the object/property, applicable rule, actual location, expected
location when known, and a possible repair. For example:

```text
Assay "assay-a" is not in the folder defined by the workspace.
Found:    /experiments/assay-a/dataset.yml
Expected: /assays/assay-a/dataset.yml
Action:   Move to expected folder
```

Applications validate destinations before writing and can prompt when decisions
are needed. Reading and validation do not modify files; moves, registration, and
reference updates are explicit repair operations.

## Publication

Publication generates an independent, **static artifact** from a particular
workspace state. Later workspace edits do not alter it. Validation and the virtual
Unmanaged folder belong to workspace-management applications.

Only eligible model content and eligible payload dependencies are selected;
invalid and unmanaged content are excluded. Copying a directory wholesale is
insufficient: it may contain unregistered files. A metadata file containing both
eligible and excluded records likewise needs filtering or rejection. Generated
metadata must not contain broken required dependencies caused by exclusion.

## Open decisions

- **Discovery and variables:** Locating the definition, enumerating dynamic paths
  such as `/assays/*/dataset.yml`, recognizing misplaced metadata, and defining
  scoped variables and parent references. A write expression alone does not define
  how to discover its objects on read.
- **Representation:** Custom parsers and field extraction, literal constants,
  shared-file updates, and preserving order and duplicate occurrences when a
  collection is split across files or folders.
- **Placement extensions:** File discovery, reusable rules, and extension-based
  routing such as CSV to `/data/csv` and FASTA to `/data/fasta`.
- **Publication and updates:** Artifact encoding, consistent-state capture,
  aborting versus publishing a valid subset when errors exist, external resources,
  fragment handling, and stale-file cleanup after changes.
