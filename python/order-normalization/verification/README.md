# Verification

Run this pack through the framework verifier before using it in a benchmark:

```powershell
dotnet run --project C:\Work\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path C:\Work\model-validator-challenges\python\order-normalization
```

The generated `pack-verification.json` file is ignored by Git.
