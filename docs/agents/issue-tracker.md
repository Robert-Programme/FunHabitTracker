# Issue tracker: GitHub

Issues and specs for this repository live as GitHub issues under
`Robert-Programme/FunHabitTracker`. Use the `gh` CLI for all operations.

## Conventions

- Create: `gh issue create --title "..." --body "..."`.
- Read: `gh issue view <number> --json number,title,body,labels,comments`.
- List: `gh issue list --state open --json number,title,body,labels,comments`.
- Comment: `gh issue comment <number> --body "..."`.
- Apply or remove labels: `gh issue edit <number> --add-label "..."` or
  `--remove-label "..."`.
- Close: post the explanation first, then run `gh issue close <number>`.

Prefer GitHub sub-issues and native issue dependencies for parent-child and
blocking relationships. If those features are unavailable, use
`Part of #<number>` and `Blocked by: #<number>` in the issue body.

Before the first GitHub write in a session, run
`gh repo view --json nameWithOwner` and verify that the target is
`Robert-Programme/FunHabitTracker`. Never create issues in an upstream or
third-party repository.

## Pull requests as a triage surface

**PRs as a request surface: no.**

## Skill semantics

- "Publish to the issue tracker" means create a GitHub issue.
- "Fetch the relevant ticket" means read the corresponding GitHub issue.
- Wayfinder maps use parent issues and work packages use sub-issues.
