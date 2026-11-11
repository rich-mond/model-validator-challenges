# Customer Address Migration

Language: C# / .NET 10

Complexity: Intermediate

Evolve a populated version-one SQLite database to multiple addresses while retaining the legacy customer view and existing operations.

## Existing workspace

The starter contains a working application, local SQLite storage and a public console regression suite. Relevant components: DatabaseMigrator/schema_versions, CustomerDatabase, CustomerRepository, CustomerService, AddressRules, CustomerDirectory and CustomerExport. The public checks pass on the starter; hidden checks expose the reported defect or missing requested behaviour. Production source is under `src/CustomerAddresses`; public checks are under `tests/PublicChecks`.

## Assertions

| Assertion | Type | Capability |
| --- | --- | --- |
| `populated-upgrade` | requirement | Populated upgrade |
| `address-operations` | requirement | Address operations |
| `primary-invariants` | requirement | Primary invariants |
| `transactional-upgrade` | requirement | Transactional upgrade |
| `fresh-database` | regression | Fresh database |

Independent version-one fixtures are upgraded through the actual migrator. Checks read SQLite rows and public application responses, reopen the store, and interrupt insertion of the version-two history record to verify transactional schema/data rollback. Publicly documented table/API contracts are required; the migration SQL, IDs, constraint strategy and internal design are not prescribed.

## Public self-check

```powershell
dotnet run --project tests/PublicChecks/PublicChecks.csproj -c Release
```

The workspace pins Microsoft.Data.Sqlite 10.0.5 and its lock files. Validator image construction restores dependencies; assertion execution runs without network access using the image's package cache. Compiler warnings are errors. Keep the pinned packages; no external database or service is required.

## Qualify

From the framework repository:

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- challenge verify --path ../model-validator-challenges/dotnet/customer-address-migration
```

Qualification is distinct from agent performance: it rejects the starter and known-invalid counterexamples, and requires three stable passing oracle runs. The oracle patch is never a scoring input for actual candidates.

## Run a candidate

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- benchmark --challenge ../model-validator-challenges/dotnet/customer-address-migration --output ../model-validator-runs/customer-address-migration-interactive
```

For an actual Codex CLI attempt, follow [the local Codex instructions](../../README.md#local-codex-smoke-runs) in the catalogue README (from this pack use `../../README.md` to reach the catalogue). Select a model actually configured in your installation. Every attempt must use fresh output and run the declared public check.

## Validation boundary

The trusted supervisor compares concrete results after candidate code runs in a disposable worker. Early successful exit is not assertion completion. Worker and supervisor share a container; this is not protection against arbitrary hostile-code interference. Process-adapter model runs remain non-authoritative local smoke tests. Initial author-initiated runs are not independent model-performance evidence.
