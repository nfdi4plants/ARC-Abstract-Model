---
title: Abstract Model Profiles
category: Specification
categoryindex: 3
index: 1
---

# Abstract Model Profiles

The ARC Abstract Model specification defines three profiles that together form the general ARC RDM model: Provenance Core, Semantic Core, and Administrative Core. The implementation uses one unified object model; the Abstract Model profiles describe coherent subsets of the same model surface.

Entity specifications shared unchanged between Abstract Model profiles live in `shared/`: [Data](shared/Data.md), [Sample](shared/Sample.md), [Annotation](shared/Annotation.md), [FormalParameter](shared/FormalParameter.md), [DefinedTerm](shared/DefinedTerm.md), and [DefinedTermSet](shared/DefinedTermSet.md). Entities whose fields differ between profiles retain their full tables in each profile folder.

## Reading Order

1. [Provenance Core](provenance_core/overview.md)
2. [Semantic Core](semantic_core/overview.md)
3. [Administrative Core](administrative_core/overview.md)

## Profile Discriminators

Datasets MUST declare their Abstract Model profiles in `conformsTo` using the corresponding case-sensitive discriminator:

| Abstract Model profile | Discriminator |
|--------------|---------------|
| [Administrative Core](administrative_core/overview.md) | `administrative-core` |
| [Semantic Core](semantic_core/overview.md) | `semantic-core` |
| [Provenance Core](provenance_core/overview.md) | `provenance-core` |

Other classifications or specializations MAY also be present in `conformsTo`. The `type` field identifies the entity type; the Abstract Model profile discriminator identifies the applicable profile-specific specification.

### Dataset Nesting

A Dataset's `hasParts` collection MAY contain Datasets from any Abstract Model profile, including different profiles within the same collection. Each child MUST declare its own discriminator and follow the corresponding [Administrative Core Dataset](administrative_core/Dataset.md), [Provenance Core Dataset](provenance_core/Dataset.md), or [Semantic Core Dataset](semantic_core/Dataset.md) specification. A child's profile is independent of its parent's and siblings' profiles. These nesting options apply recursively to every child's `hasParts`.

## Principles

- Process-centric: experiments and workflows are modeled as processes connecting inputs to outputs.
- Unified: Provenance Core, Semantic Core, and Administrative Core properties are available on shared model types rather than split into separate runtime objects.
- Extensible: `Annotation` and `additionalTypes` carry domain-specific information while typed properties cover the common profile surface.
- Representation-aware but model-first: SQL and YAML schemas derive from the markdown spec.