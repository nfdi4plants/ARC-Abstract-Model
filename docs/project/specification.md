---
title: Specification Guide
category: Project
categoryindex: 2
index: 2
---

# Specification Guide

The normative specification lives in [docs/spec](../spec/index.md). The repository organizes the ARC Abstract Model into three profiles: Provenance Core, Semantic Core, and Administrative Core. Decoration profiles, including Datamap, add domain-specific refinements. The implementation remains one shared model; profile boundaries are documentation and mapping boundaries, not runtime type boundaries.

## Abstract Model

```mermaid
flowchart LR
    Dataset --processes--> Process
    Dataset --dataFiles--> Data
    Dataset --agents--> Agent
    Dataset --citations--> ScholarlyArticle
    Dataset --descriptors--> Descriptor
    Dataset --hasPart--> Dataset
    Process --inputs--> Sample
    Process --"outputs"--> Data
    Process --executesRecipe--> Recipe
    Process --parameterValue--> Annotation
    Recipe --parameters--> FormalParameter
    Recipe --components--> Annotation
    Annotation --instanceOf--> FormalParameter
    Descriptor --describes--> Sample
    Descriptor --describes--> Data
    Descriptor --annotations--> Annotation
    Agent --affiliation--> Organization
    ScholarlyArticle --authors--> Agent
```

Shared model entities:

| Entity | Source |
|--------|--------|
| Dataset | [Provenance Core](../spec/provenance_core/Dataset.md), [Semantic Core](../spec/semantic_core/Dataset.md), [Administrative Core](../spec/administrative_core/Dataset.md) |
| Process | [Process](../spec/provenance_core/Process.md) |
| Recipe | [Recipe](../spec/provenance_core/Recipe.md) |
| Sample | [Sample](../spec/shared/Sample.md) |
| Data | [Data](../spec/shared/Data.md) |
| Descriptor | [Descriptor](../spec/semantic_core/Descriptor.md) |
| Annotation | [Annotation](../spec/shared/Annotation.md) |
| FormalParameter | [FormalParameter](../spec/shared/FormalParameter.md) |
| DefinedTerm | [DefinedTerm](../spec/shared/DefinedTerm.md) |
| DefinedTermSet | [DefinedTermSet](../spec/shared/DefinedTermSet.md) |
| Agent | [Agent](../spec/administrative_core/Agent.md) |
| Organization | [Organization](../spec/administrative_core/Organization.md) |
| ScholarlyArticle | [ScholarlyArticle](../spec/administrative_core/ScholarlyArticle.md) |

Abstract Model entry points:

| Profile | Purpose | Source |
|--------------|---------|--------|
| Provenance Core | Provenance through datasets, processes, protocols, samples, data, and annotations | [Provenance Core](../spec/provenance_core/overview.md) |
| Semantic Core | Semantic descriptions that connect datasets, samples, and data with bundled annotations | [Semantic Core](../spec/semantic_core/overview.md) |
| Administrative Core | Dataset metadata, agents, affiliations, citations, licenses, and dates | [Administrative Core](../spec/administrative_core/overview.md) |

## Decorations

Decorations add domain-specific meaning through `additionalType`, specialized properties, and decoration-specific entities.

| Decoration | Purpose | Source |
|------------|---------|--------|
| ISA | Investigation, Study, Assay, Source, Sample, and ISA property value roles | [ISA](../spec/decorations/isa/overview.md) |
| Workflow Run | Workflow and Run datasets, workflow protocols, and workflow invocations | [Workflow Run](../spec/decorations/workflow-run/overview.md) |
| Datamap | Data files, selected fragments, and DataContext descriptors | [Datamap](../spec/decorations/datamap/overview.md) |

## Naming Notes

The Provenance Core vocabulary uses `Process` and `Recipe`, not the older placeholder names `Process` and `Protocol`.

For process I/O, the current ARC Abstract Model and YAML schema names are:

- `inputs`
- `outputs`
- `executesRecipe`
- `parameterValue`
- `additionalProperty`

Some legacy/profile-shaped examples and upstream references use RO-Crate or Bioschemas names such as `object` and `result`. See [Examples and schemas](examples-and-schemas.md) for how those files are treated.

## Querying

The query use cases are described in [Querying](querying.md). Traversal, path queries, and graph indexes are requirements for future operational layers and remain outside ARCBaseModel Layer 1.


