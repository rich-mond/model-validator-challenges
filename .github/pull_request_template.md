## Summary

- 
- 
- 

## Repository Boundary

- [ ] This challenge repo contains challenge-pack inputs only.
- [ ] No benchmark runs, generated verification JSON, target outputs, logs or temporary workspaces are committed here.

## Challenge Packs

- 

## GitFlow

- Base branch: `develop`
- Head branch: `feature/...`, `fix/...`, `release/...` or `hotfix/...`
- [ ] This PR does not target `main` directly unless it is an explicit release or hotfix PR.

## Validation

List the exact commands run and their outcomes. Use `not run` with a reason when appropriate.

```powershell
dotnet run --project ..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .\dotnet\idempotent-processing
dotnet run --project ..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .\python\order-normalization
```

## Generated Outputs

- [ ] Generated benchmark outputs, verification JSON, logs and temporary workspaces are not committed.
