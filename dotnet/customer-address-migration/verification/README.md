# Qualification

From `model-validator` alongside this catalogue:

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- challenge verify --path ../model-validator-challenges/dotnet/customer-address-migration
```

The verifier checks manifest/input digests and base identity, rejects at least one required assertion on the starter, checks the oracle three times and enforces every counterexample's declared behavioural failures. See `counterexamples/manifest.json` for the exact expected assertion groups. Generated JSON is ignored and is not evidence of a real model solving the task.

Independent version-one fixtures are upgraded through the actual migrator. Checks read SQLite rows and public application responses, reopen the store, and interrupt insertion of the version-two history record to verify transactional schema/data rollback. Publicly documented table/API contracts are required; the migration SQL, IDs, constraint strategy and internal design are not prescribed.

Candidate call results are compared in a separate supervisor; an empty response, non-zero exit, timeout or malformed response cannot pass. The existing shared-container security limitations still apply. CI qualifies this pack using the catalogue's fixed framework revision, without model API calls.
