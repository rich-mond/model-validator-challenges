# Idempotent Processing

Language: C# / .NET

This challenge asks a target agent to make a simple command ledger idempotent.

The starter workspace applies every delivered command. That is wrong when the same command ID is delivered twice or delivered concurrently. A correct solution must apply each command ID at most once while preserving normal balance updates for distinct commands.

## Assertions

| Assertion | Type | What It Checks |
| --- | --- | --- |
| `duplicate-delivery` | requirement | The same command ID is ignored after it has already been applied |
| `concurrent-delivery` | requirement | Concurrent delivery of the same command is safe |
| `distinct-commands` | regression | Distinct command IDs still update balances normally |

## Public Self-Check

The materialised starter workspace includes a small public check project. Target agents are instructed to run it before they stop:

```powershell
dotnet run --project src\CommandProcessor.Checks\CommandProcessor.Checks.csproj
```

This is a local sanity check. The hidden Docker validator remains the authoritative benchmark result.

## Verify

```powershell
dotnet run --project ..\..\..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .
```
