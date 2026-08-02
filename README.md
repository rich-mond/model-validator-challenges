# Model Validator Challenges

This repository contains challenge packs for the Model Validator framework.

It is intentionally separate from the framework repository. The framework repository contains the runner and reporting engine. This repository contains challenge inputs: starter bundle, task prompt, hidden validator, oracle patch and known-invalid patches.

The repository is language-agnostic at the top level. Individual challenge packs may target a specific ecosystem because their starter workspaces and validators need real tooling.

Suggested layout:

```text
dotnet/
  idempotent-processing/
python/
javascript/
java/
rust/
```

## Current Challenge

```text
dotnet/idempotent-processing/
├── challenge.json
├── prompt.md
├── workspace/starter.bundle
├── validator/
├── oracle/solution.patch
├── counterexamples/
└── verification/
```

The current challenge asks an agent to make command processing idempotent under duplicate and concurrent delivery.

## How It Is Used

From a clone of the framework repository:

```text
modelval challenge verify --path ../model-validator-challenges/dotnet/idempotent-processing
modelval run --plan <benchmark-plan.json>
```

A real benchmark plan points at this challenge directory and one or more target configuration files. The framework materialises `workspace/starter.bundle` into a fresh workspace for each target, runs the target adapter, captures the candidate patch, then runs the validator assertions declared in `challenge.json`.

## What The Target Agent Sees

The target agent receives only:

- the materialised starter workspace;
- the task prompt;
- explicitly allowed runtime configuration.

The target agent does not receive:

- validator source;
- oracle patch;
- counterexamples;
- `challenge.json`;
- Git remotes or repository credentials.

## What The Validator Does

The validator is built from `validator/Containerfile`. Each assertion in `challenge.json` runs `/validator/run <assertion-id>` against the candidate workspace mounted at `/candidate`.

Correctness is determined only by assertion exit statuses. The oracle patch is not used to judge a real candidate.

## Generated Outputs

Verification JSON, run outputs, logs and temporary workspaces are generated artifacts. They are ignored by Git and should not be committed to this repository.
