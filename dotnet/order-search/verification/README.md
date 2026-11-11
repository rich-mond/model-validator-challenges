# Qualification

From `model-validator` alongside this catalogue:

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- challenge verify --path ../model-validator-challenges/dotnet/order-search
```

The verifier checks manifest/input digests and base identity, rejects at least one required assertion on the starter, checks the oracle three times and enforces every counterexample's declared behavioural failures. See `counterexamples/manifest.json` for the exact expected assertion groups. Generated JSON is ignored and is not evidence of a real model solving the task.

The worker hosts the real application on an ephemeral loopback Kestrel port and seeds SQLite independently. The supervisor compares actual HTTP statuses, DTOs, filtered counts and ordered page contents. Authentication maps known local bearer tokens to server-owned tenant claims; query parameters and arbitrary tenant headers cannot choose an identity.

Candidate call results are compared in a separate supervisor; an empty response, non-zero exit, timeout or malformed response cannot pass. The existing shared-container security limitations still apply. CI qualifies this pack using the catalogue's fixed framework revision, without model API calls.
