# Verification

Run this pack through the framework verifier before using it in a benchmark:

```powershell
dotnet run --project ..\..\..\..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..
```

The generated `pack-verification.json` file is ignored by Git.

Version 1.1.0 calibrates conflicting duplicate prices in both price orders, with a leading zero-quantity line that must not establish the price. The oracle must pass the public mixed-price test and hidden assertions. `last-duplicate-price` and `zero-quantity-price` must fail `duplicate-lines`, alongside the established counterexamples. The starter remains intentionally incorrect.
