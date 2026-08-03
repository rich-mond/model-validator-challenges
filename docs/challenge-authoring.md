# Challenge Authoring

This document is the technical contract for adding or maintaining a Model Validator challenge pack.

The top-level README explains how to use the existing packs. This file explains how a pack is built, what each file means and how the framework validates it.

## Pack Layout

A complete challenge pack lives under a language or ecosystem folder:

```text
<language>/<challenge-id>/
|-- challenge.json
|-- prompt.md
|-- workspace/
|   `-- starter.bundle
|-- validator/
|   |-- Containerfile
|   `-- run
|-- oracle/
|   `-- solution.patch
|-- counterexamples/
|   |-- manifest.json
|   `-- *.patch
`-- verification/
    `-- README.md
```

The language folder is catalog organization only. The framework does not infer behavior from the folder name. The `challenge.json` manifest is the contract.

## Manifest

`challenge.json` declares the immutable inputs and validation contract for one pack.

Core fields:

- `schemaVersion`: manifest schema version, currently `1.0`.
- `challengeId`: stable challenge identifier.
- `displayName`: human-readable challenge name.
- `prompt.path`: path to the prompt file relative to the challenge root.
- `prompt.sha256`: SHA-256 digest of the prompt file.
- `workspace.path`: path to the starter Git bundle.
- `workspace.sha256`: SHA-256 digest of the starter Git bundle.
- `workspace.baseCommit`: expected commit SHA after the starter bundle is materialised.
- `validation.image`: validator container reference or local build context.
- `validation.workspacePath`: mount path where validators expect the candidate workspace.
- `validation.assertions`: objective checks run after target execution.
- `selfChecks`: public workspace commands the target agent must run before it stops.
- `limits`: timeout and resource limits used by target and validation execution.

Every assertion and self-check command is an executable plus argument array. Do not encode shell command strings in the manifest.

Self-checks are copied into `MODEL_VALIDATOR_TASK.md` for interactive benchmark runs. They should be useful public checks such as `dotnet test`, `python -m unittest` or a project-specific smoke test. They are not hidden validators and do not determine benchmark correctness.

## Prompt

`prompt.md` is the only task description given to the target agent.

It should state:

- the requested behavior;
- any public constraints;
- the expected edit boundary.

Put local build or test commands in `selfChecks`, not only in prose, so editor-based agents receive explicit commands in the generated workspace task.

It must not reveal hidden validator implementation details, oracle patches or known-invalid examples.

After editing the prompt, recompute its SHA-256 and update `challenge.json`.

## Starter Workspace

`workspace/starter.bundle` is a Git bundle containing the exact starter repository.

The framework materialises it into a fresh candidate workspace for each attempt, checks the base commit and removes Git remotes before target execution.

The starter should contain only files the target agent is allowed to see. Do not include validators, oracle solutions, calibration patches, credentials or generated outputs in the starter bundle.

After rebuilding the bundle, recompute its SHA-256 and update `challenge.json`.

## Validator

`validator/` is a Docker build context for authoritative checks.

Validators may use any runtime needed by the challenge. The framework only builds or resolves the validator image, then invokes the declared assertion commands with the candidate workspace mounted at `validation.workspacePath`.

Validator rules:

- Exit code `0` means the assertion passed.
- Non-zero exit codes mean failed assertions unless the framework classifies timeout or infrastructure failure.
- Validation must be deterministic.
- Validation must not depend on network access.
- Validation must not use model APIs or human judgement.

## Oracle Patch

`oracle/solution.patch` is a known-good patch used only to calibrate the challenge pack.

The framework applies the oracle to a materialised starter workspace during `challenge verify`. The oracle must pass every required assertion and must produce stable results across repeated validation runs.

The oracle is not used to score real candidates.

## Counterexamples

`counterexamples/` contains known-invalid patches that prove the validator catches important failure modes.

`counterexamples/manifest.json` maps each patch to the assertion IDs it is expected to fail. Each counterexample should be small and targeted.

Counterexamples are calibration evidence. They are not mounted into target-agent workspaces.

## Verification

Run pack verification after changing any prompt, starter bundle, validator, oracle patch, counterexample or manifest:

```powershell
dotnet run --project ..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .\<language>\<challenge-id>
```

Verification checks:

- manifest validity;
- prompt digest;
- starter bundle digest;
- starter base commit;
- validator image build or digest-pinned reference;
- starter fails at least one required assertion;
- oracle passes all assertions;
- oracle remains stable across three runs;
- counterexamples fail the assertions declared in their manifest.

Generated verification JSON is ignored and must not be committed.

## Generated Outputs

Challenge repositories should contain challenge-pack inputs only.

Do not commit:

- benchmark run directories;
- generated target or plan JSON;
- validator run logs;
- candidate patches;
- temporary workspaces;
- generated verification JSON;
- local notes such as `AGENTS.md`;
- `.bak` files.

Use ignored directories such as `runs/`, `artifacts/`, `outputs/` or `temp/` for local execution evidence.
