# Domain Docs

Before exploring the codebase, agents read these files when they exist:

- `GLOSSARY.md` at the repository root.
- Relevant architecture decisions under `docs/adr/`.

Proceed silently when either location does not exist. Do not suggest creating
domain documentation in advance. Create it only when terminology or an
architecture decision is actually resolved.

## File structure

This repository uses a single-context layout:

```text
/
|-- GLOSSARY.md
|-- docs/adr/
`-- src/
```

## Use the glossary vocabulary

Use terms as defined in `GLOSSARY.md` in issue titles, proposals, hypotheses,
tests, and code. Do not drift to synonyms that the glossary explicitly avoids.
If a required concept is missing, reconsider whether the new term is necessary
or record the gap for domain modeling.

## Flag ADR conflicts

If proposed work contradicts an existing ADR, identify the conflict explicitly
instead of silently overriding the decision.
