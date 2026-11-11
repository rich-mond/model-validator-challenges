# Qualification

From `model-validator` alongside this catalogue:

```powershell
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- challenge verify --path ../model-validator-challenges/dotnet/order-lifecycle-regression
```

The verifier checks manifest/input digests and base identity, rejects at least one required assertion on the starter, checks the oracle three times and enforces every counterexample's declared behavioural failures. See `counterexamples/manifest.json` for the exact expected assertion groups. Generated JSON is ignored and is not evidence of a real model solving the task.

The validator holds real repository read snapshots until both requests have been initiated, then releases them without waiting for two reads. This exposes the starter lost update deterministically and permits correct serialization as well as conditional database transitions. Both shared and separate service instances are checked; neither winner is prescribed.

Candidate call results are compared in a separate supervisor; an empty response, non-zero exit, timeout or malformed response cannot pass. The existing shared-container security limitations still apply. CI qualifies this pack using the catalogue's fixed framework revision, without model API calls.

`original-race.patch` is intentionally empty: it selects the defective starter unchanged. The other counterexamples fix state selection but log a losing transition, or incorrectly reject a legitimate completed-operation retry.
