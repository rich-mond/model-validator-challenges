# Challenge Complexity

Challenge complexity is catalog metadata. It is independent of language and describes how much reasoning, codebase navigation and verification discipline a pack demands from the target agent.

## Levels

| Level | Name | Expected Shape |
| --- | --- | --- |
| 1 | Foundation | Small, focused change in one or two files; public self-checks expose the main behavior; useful for smoke testing the benchmark workflow |
| 2 | Focused | Several related changes across a small project; requires understanding local structure, edge cases and at least one regression path |
| 3 | Intermediate | Multi-file implementation with internal design choices, data flow across components and meaningful negative cases |
| 4 | Advanced | Cross-cutting behavior in a larger starter workspace; requires refactoring, preserving compatibility and handling concurrency, persistence or integration boundaries |
| 5 | Stress | Ambiguous real-world task with broad blast radius, multiple valid designs, hidden edge cases and substantial regression risk |

## Classification Rules

Classify a pack by the work a target agent must perform, not by the implementation language.

Use the lowest level that honestly describes the challenge. A pack is not advanced just because its validator is complex; complexity is about the candidate-visible task and workspace.

When a pack changes, re-check the complexity label. If public self-checks or starter structure make the task materially easier or harder, update the top-level README and per-pack README in the same PR.

## Current Catalog

| Pack | Language | Complexity | Reason |
| --- | --- | --- | --- |
| `dotnet/idempotent-processing` | C# / .NET | Foundation | Single-library behavior fix with a public test project and hidden validator coverage |
| `dotnet/checkout-discounts` | C# / .NET | Intermediate | Multi-file pricing task with tier discounts, coupon stacking, injected-clock validity and hidden source-boundary validation |
| `python/order-normalization` | Python | Foundation | Single-function normalization task with public unit tests and hidden validator coverage |
