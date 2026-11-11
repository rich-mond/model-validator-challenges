# Cross-Layer Order Search

Language: C# / .NET 10

Complexity: Intermediate

Implement authenticated, paginated search across an existing Minimal API, application service and SQLite query layer.

## Existing workspace

The starter contains a working application, local SQLite storage and a public console regression suite. Relevant components: OrderApplication, OrderEndpoints, OrderService, OrderRepository, OrderDatabase, DemoAuthentication, TenantIdentity, DTOs and existing input validation. The public checks pass on the starter; hidden checks expose the reported defect or missing requested behaviour. Production source is under `src/OrderSearch`; public checks are under `tests/PublicChecks`.

## Assertions

| Assertion | Type | Capability |
| --- | --- | --- |
| `search-filters` | requirement | Search filters |
| `pagination-and-ordering` | requirement | Pagination and ordering |
| `tenant-and-retrieval` | regression | Tenant and retrieval |
| `invalid-requests` | requirement | Invalid requests |

The worker hosts the real application on an ephemeral loopback Kestrel port and seeds SQLite independently. The supervisor compares actual HTTP statuses, DTOs, filtered counts and ordered page contents. Authentication maps known local bearer tokens to server-owned tenant claims; query parameters and arbitrary tenant headers cannot choose an identity.

## Public self-check

```powershell
dotnet run --project tests/PublicChecks/PublicChecks.csproj -c Release
```

The workspace pins Microsoft.Data.Sqlite 10.0.5 and its lock files. Validator image construction restores dependencies; assertion execution runs without network access using the image's package cache. Compiler warnings are errors. Keep the pinned packages; no external database or service is required.

## Qualify

From the framework repository:

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- challenge verify --path ../model-validator-challenges/dotnet/order-search
```

Qualification is distinct from agent performance: it rejects the starter and known-invalid counterexamples, and requires three stable passing oracle runs. The oracle patch is never a scoring input for actual candidates.

## Run a candidate

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- benchmark --challenge ../model-validator-challenges/dotnet/order-search --output ../model-validator-runs/order-search-interactive
```

For an actual Codex CLI attempt, follow [the local Codex instructions](../../README.md#local-codex-smoke-runs) in the catalogue README (from this pack use `../../README.md` to reach the catalogue). Select a model actually configured in your installation. Every attempt must use fresh output and run the declared public check.

## Validation boundary

The trusted supervisor compares concrete results after candidate code runs in a disposable worker. Early successful exit is not assertion completion. Worker and supervisor share a container; this is not protection against arbitrary hostile-code interference. Process-adapter model runs remain non-authoritative local smoke tests. Initial author-initiated runs are not independent model-performance evidence.
