---
title: LinkML Workspace Examples
category: Specification
categoryindex: 3
index: 7
---

# LinkML workspace examples

An alternative to the [workspace draft](workspace-definition.md): use LinkML for
the ARC model and selector expressions, and attach the layout as a structured
schema annotation. The `$...` storage instructions still belong to our proposed
workspace engine. These are design examples, not an implemented LinkML plugin.

LinkML supports structured [annotations](https://linkml.io/linkml/schemas/annotations.html).
Its [class expressions](https://linkml.io/linkml-model/latest/docs/AnonymousClassExpression/)
provide `slot_conditions`; [has_member](https://linkml.io/linkml-model/latest/docs/has_member/)
expresses collection membership. Using those expressions to choose destinations
is the extension proposed here.

## Root metadata, data files, and assay folders

This is a fragment to add to an ARC LinkML schema. The complete translation of
the Markdown entity specifications into LinkML is a separate prerequisite; it is
not supplied by these examples. `ws` is an illustrative extension namespace.
The root layout is abbreviated; other populated properties need bindings too.

```yaml
prefixes:
  ws: https://example.org/arc/workspace/

annotations:
  ws:layout:
    tag: ws:layout
    value:
      type: metadata/type.yml
      conformsTo: metadata/conformsTo.yml
      identifiers: metadata/identifiers.yml
      title: metadata/title.yml
      agents: metadata/agents.yml
      citations: metadata/citations.yml

      dataFiles:
        - $directory: /data
          $when:
            slot_conditions:
              type: {required: true, equals_string: Data}
          $file: data.yml

      hasParts:
        - metadata/hasParts.yml
        - $directory: /assays
          $when:
            slot_conditions:
              type: {required: true, equals_string: Dataset}
              additionalTypes:
                required: true
                has_member:
                  equals_string: assay
          $each:
            $directory: "${item.identifiers[0]}"
            $file: dataset.yml
```

The annotation's `value` retains the Dataset-shaped template. The main syntax
change is `$when`: it now contains a LinkML class expression instead of our
ad hoc equality and `$contains` notation. The engine evaluates these expressions
in the context of the ARC schema's class and slot definitions. `required: true`
makes missing selector values fail the condition.

The proposed engine interprets this as:

- Root properties are read and written individually under `/metadata`.
- `/data/data.yml` holds a sequence of Data records; their payloads belong in `/data`.
- Matching child Datasets use `/assays/<identifier>/dataset.yml`, one object per file.
- Unmatched children use the sequence in `/metadata/hasParts.yml`.

Rule exclusivity, fallback precedence, relative scopes, and publication eligibility
remain the workspace draft's behavior. LinkML does not execute those operations.

## CSV and FASTA destinations

Replace `dataFiles` inside the annotation's `value` with this variant. It keeps
the metadata together while using LinkML string patterns to select destinations:

```yaml
dataFiles:
  $file: metadata/dataFiles.yml
  $each:
    path:
      $rules:
        - $when:
            slot_conditions:
              path:
                required: true
                pattern: '.*\.[cC][sS][vV]$'
          $directory: /data/csv

        - $when:
            slot_conditions:
              path:
                required: true
                pattern: '.*\.[fF][aA]([sS][tT][aA])?$'
          $directory: /data/fasta
```

Here the engine evaluates each condition against the current Data object's
incoming or recorded `path`, then preserves the basename in the chosen folder.
The patterns recognize `.csv`, `.fa`, and `.fasta`, ignoring case. Exactly one
route must match. [pattern](https://linkml.io/linkml-model/latest/docs/pattern/)
is a LinkML constraint; `$rules` and file placement are workspace extensions.

## Individual fields and parent variables

Inside the assay rule, replace the whole-object `$each` with property bindings:

```yaml
$each:
  $directory: "${item.identifiers[0]}"
  type: metadata/type.yml
  additionalTypes: metadata/additionalTypes.yml
  conformsTo: metadata/conformsTo.yml
  identifiers: metadata/identifiers.yml
  title: metadata/title.yml
  agents: metadata/agents.yml
  citations: metadata/citations.yml
  dataFiles: metadata/dataFiles.yml
  hasParts: metadata/hasParts.yml
```

Other populated properties need bindings too. For parent-based grouping, a possible
inner directory expression is `${parent.identifiers[0]}/${item.identifiers[0]}`.
In this proposal, `parent` denotes the Dataset owning the selected collection.
These expressions and lexical scopes are ours, not LinkML expression syntax;
their complete definition and reverse discovery remain open.

## Where LinkML-Map fits

LinkML-Map can transform already-parsed custom metadata into ARC-shaped values.
For example, a table parser might produce ContributorRow objects with `full_name`
and an `email_addresses` sequence:

```yaml
class_derivations:
  Agent:
    populated_from: ContributorRow
    slot_derivations:
      type:
        value: Agent
      name:
        populated_from: full_name
      emails:
        populated_from: email_addresses
```

These are actual [LinkML-Map mapping constructs](https://linkml.io/linkml-map/writing-specs/),
assuming the source and target schemas are supplied. `populated_from` names a
class or field, not a file. The workspace engine locates the source, invokes its
parser and mapping, and handles writes. An arbitrary read mapping does not imply
a working inverse writer; [automatic inversion](https://linkml.io/linkml-map/)
supports only certain transformations.

## What this provides

| LinkML / LinkML-Map | Workspace engine still required |
|---|---|
| ARC classes, slots, and value constraints | Filesystem discovery and ownership |
| A standard representation for selector conditions | Evaluating guards in scope and resolving fallback rules |
| Schema annotations containing layout metadata | Validating and executing the layout language |
| Transformations between parsed data models | Coordinated reads, writes, repairs, and static publication generation |

The ARC LinkML schema must retain optional IDs, required identifier collections,
and the combined Dataset profile surface. In particular, do not mark ARC `id`
as LinkML `identifier: true`: that would make it mandatory and unique.
Use ordinary slots and ordered, non-unique collections instead.
[Slot semantics](https://linkml.io/linkml/schemas/slots.html),
[list order](https://linkml.io/linkml-model/latest/docs/list_elements_ordered/),
[list uniqueness](https://linkml.io/linkml-model/latest/docs/list_elements_unique/).

Annotations do not automatically validate the embedded layout. A separate
LinkML schema could describe the template language and validate its extracted
value; the documented `instantiates` annotation mechanism does not currently
enforce this by itself.
[Annotation validation](https://linkml.io/linkml/schemas/annotations.html#validation-of-annotations)

Checks for this exploration cover YAML syntax and documentation links. LinkML
and LinkML-Map are not installed here, so these fragments have not been executed
or checked with their runtimes.
