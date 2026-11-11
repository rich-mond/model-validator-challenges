# Order Lifecycle Regression

Language: C# / .NET 10

Complexity: Intermediate

Debug an existing cancellation/dispatch race while preserving persistence and audit semantics.

## Existing workspace

The starter contains a working application, local SQLite storage and a public console regression suite. Relevant components: OrderService, IOrderRepository/OrderRepository, OrderDatabase, AuditLog, OrderQueries, OrderAdministration and OrderReceipt. The public checks pass on the starter; hidden checks expose the reported defect or missing requested behaviour. Production source is under `src/OrderLifecycle`; public checks are under `tests/PublicChecks`.

## Assertions

| Assertion | Type | Capability |
| --- | --- | --- |
| `ordinary-transitions` | requirement | Ordinary transitions |
| `retry-and-guard` | requirement | Retry and guard |
| `concurrent-transition` | requirement | Concurrent transition |
| `existing-behaviour` | regression | Existing behaviour |

The validator holds real repository read snapshots until both requests have been initiated, then releases them without waiting for two reads. This exposes the starter lost update deterministically and permits correct serialization as well as conditional database transitions. Both shared and separate service instances are checked; neither winner is prescribed.

## Public self-check

```powershell
dotnet run --project tests/PublicChecks/PublicChecks.csproj -c Release
```

The workspace pins Microsoft.Data.Sqlite 10.0.5 and its lock files. Validator image construction restores dependencies; assertion execution runs without network access using the image's package cache. Compiler warnings are errors. Keep the pinned packages; no external database or service is required.

## Qualify

From the framework repository:

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- challenge verify --path ../model-validator-challenges/dotnet/order-lifecycle-regression
```

Qualification is distinct from agent performance: it rejects the starter and known-invalid counterexamples, and requires three stable passing oracle runs. The oracle patch is never a scoring input for actual candidates.

## Run a candidate

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- benchmark --challenge ../model-validator-challenges/dotnet/order-lifecycle-regression --output ../model-validator-runs/order-lifecycle-regression-interactive
```

For an actual Codex CLI attempt, follow [the local Codex instructions](../../README.md#local-codex-smoke-runs) in the catalogue README (from this pack use `../../README.md` to reach the catalogue). Select a model actually configured in your installation. Every attempt must use fresh output and run the declared public check.

## Validation boundary

The trusted supervisor compares concrete results after candidate code runs in a disposable worker. Early successful exit is not assertion completion. Worker and supervisor share a container; this is not protection against arbitrary hostile-code interference. Process-adapter model runs remain non-authoritative local smoke tests. Initial author-initiated runs are not independent model-performance evidence.
