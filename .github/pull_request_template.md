## Summary

<!-- Explain what changed and why. Keep this human-readable for a reviewer coming in cold. -->

## Repository Boundary

<!-- Confirm this challenge repo contains challenge-pack inputs only. No benchmark runs, generated verification JSON, target outputs, logs or temporary workspaces should be committed here. -->

## Challenge Packs

<!-- List affected packs, for example `dotnet/idempotent-processing` or `python/order-normalization`. -->

## GitFlow

- Base branch: `develop`
- Head branch: <!-- feature/... fix/... release/... hotfix/... -->
- This PR does not target `main` directly.

## Validation

<!-- List the challenge verification commands actually run and their outcomes. Use "not run" with a reason when appropriate. -->

```powershell
dotnet run --project C:\Work\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path C:\Work\model-validator-challenges\<language>\<challenge-id>
```

## Generated Outputs

<!-- Confirm generated runs, artifacts, logs, verification JSON and temporary workspaces were not committed. -->
