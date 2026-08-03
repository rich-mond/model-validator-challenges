# Verification

Run this pack through the framework verifier before using it in a benchmark:

```powershell
dotnet run --project ..\..\..\..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..
```

The generated `pack-verification.json` file is ignored by Git.
