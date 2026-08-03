# Model Validator Challenges

This repository is the public challenge catalog for the Model Validator framework.

It contains challenge packs: the task prompts, starter workspaces, validators and calibration patches used to evaluate coding-agent output. It does not contain the runner. The runner lives in:

```text
https://github.com/rich-mond/model-validator
```

## Why This Repo Exists

Model Validator deliberately separates the benchmark framework from the challenge packs.

The framework repo answers: how do we materialise workspaces, run target adapters, isolate validation, score results and report comparisons?

This repo answers: what tasks should agents attempt, what starting files do they receive, and how is correctness objectively verified?

Keeping those responsibilities separate matters because challenge packs can be written for any language or ecosystem without changing the framework. A new Python, JavaScript, Java or Rust task should be another pack in this repository, not a framework feature.

## Supported Challenge Packs

| Pack | Language | Task | Verification |
| --- | --- | --- | --- |
| `dotnet/idempotent-processing` | C# / .NET | Make command processing idempotent under duplicate and concurrent delivery | Docker validator checks duplicate delivery, concurrent delivery and distinct-command regression |
| `python/order-normalization` | Python | Normalize inbound order events without mutating input | Docker validator checks duplicate SKU aggregation, decimal cent rounding and metadata/input stability |

Each listed pack is expected to pass `modelval challenge verify` before it is used in a benchmark.

## Fresh Start

Clone the framework and challenge repos side by side:

```powershell
cd C:\Work
git clone https://github.com/rich-mond/model-validator.git
git clone https://github.com/rich-mond/model-validator-challenges.git
```

Build the framework:

```powershell
cd C:\Work\model-validator
dotnet restore ModelValidator.slnx --locked-mode
dotnet build ModelValidator.slnx -c Release
dotnet test ModelValidator.slnx -c Release --no-build
```

Verify both supported packs:

```powershell
dotnet run --project C:\Work\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path C:\Work\model-validator-challenges\dotnet\idempotent-processing
dotnet run --project C:\Work\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path C:\Work\model-validator-challenges\python\order-normalization
```

Verification proves a challenge pack is internally coherent before any model or agent is benchmarked. The starter workspace must fail required assertions, the oracle patch must pass, repeated oracle validation must be stable and known-invalid patches must fail.

## Pack Anatomy

Every challenge pack has the same contract:

```text
<language>/<challenge-id>/
├── challenge.json
├── prompt.md
├── workspace/
│   └── starter.bundle
├── validator/
│   ├── Containerfile
│   └── run
├── oracle/
│   └── solution.patch
├── counterexamples/
│   ├── manifest.json
│   └── *.patch
└── verification/
    └── README.md
```

The language folder names are catalog organization only. The framework does not infer behavior from them. The pack manifest is the contract.

## What Each File Means

`challenge.json` declares the challenge ID, prompt digest, starter bundle digest, validator image, assertions and time limits.

`prompt.md` is the task text passed to the target agent.

`workspace/starter.bundle` is a Git bundle containing the exact starter repository. The framework materialises this bundle into a fresh candidate workspace for each target attempt.

`validator/` contains a Docker build context for the hidden validator. The target agent does not see these files during execution.

`oracle/solution.patch` is a known-good patch used to prove the validator can recognize a correct solution. It is not used to judge real candidates.

`counterexamples/` contains known-bad patches used to calibrate validator sensitivity.

`verification/` contains human notes. Generated verification JSON is ignored and should not be committed.

## What The Target Agent Sees

During a benchmark, the target agent receives only:

- the materialised starter workspace;
- the task prompt;
- explicitly allowed runtime configuration.

The target agent does not receive:

- `challenge.json`;
- validator source;
- oracle patch;
- counterexamples;
- repository credentials;
- Git remotes.

The framework removes Git remotes from the materialised workspace before target execution.

## Run A Benchmark Against A Pack

Most users do not write target JSON or adapter scripts. From the framework repo, run `benchmark` with the challenge path, agent and model:

```powershell
cd C:\Work\model-validator
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- benchmark --challenge C:\Work\model-validator-challenges\python\order-normalization --agent codex --model gpt-5 --output C:\Work\model-validator-runs\codex-gpt5-order-normalization
```

For the .NET challenge:

```powershell
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- benchmark --challenge C:\Work\model-validator-challenges\dotnet\idempotent-processing --agent codex --model gpt-5 --output C:\Work\model-validator-runs\codex-gpt5-idempotent-processing
```

The framework creates the candidate workspace, runs the selected coding-agent CLI, captures the candidate patch, runs this pack's hidden validator and writes the score report under the output directory.

Built-in framework presets currently support:

| Agent | Command Model Validator Runs |
| --- | --- |
| `codex` | `codex exec --model <model> --sandbox workspace-write --ask-for-approval never <prompt>` |
| `claude` | `claude -p <prompt>` |

For another agent CLI, pass the command after `--`:

```powershell
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- benchmark --challenge C:\Work\model-validator-challenges\python\order-normalization --agent custom --provider openai --model gpt-5 --output C:\Work\model-validator-runs\custom-order-normalization -- my-agent run --model {model} --prompt-file {promptPath}
```

Supported placeholders are `{prompt}`, `{promptPath}`, `{workspace}`, `{output}` and `{model}`.

## Advanced Plan-Based Use

For scripted comparisons, a benchmark plan can point at one challenge directory and one or more target configuration files:

```json
{
  "schemaVersion": "1.0",
  "planId": "first-order-normalization-run",
  "challengePath": "C:\\Work\\model-validator-challenges\\python\\order-normalization",
  "targets": [
    "C:\\Work\\targets\\codex-gpt-5-default.json"
  ],
  "attemptsPerTarget": 1,
  "outputPath": "C:\\Work\\model-validator-runs\\first-order-normalization-run",
  "execution": {
    "maximumParallelTargets": 1,
    "retainWorkspaces": false
  }
}
```

The framework then:

1. Verifies the pack manifest and digests.
2. Clones the starter bundle into a fresh workspace.
3. Removes Git remotes.
4. Gives the target adapter the workspace and prompt.
5. Waits for the adapter to finish.
6. Captures the candidate patch.
7. Runs the validator container with network disabled.
8. Writes objective results and reports to the plan output directory.

## Adding A Challenge Pack

Add a new complete pack under a language or ecosystem folder. Do not add generated benchmark output.

A complete pack needs:

- a focused task prompt;
- a starter Git bundle;
- a validator container with one or more objective assertions;
- an oracle patch that passes every required assertion;
- known-invalid patches that demonstrate important failure modes;
- a manifest with accurate SHA-256 digests.

After adding or changing a pack, run:

```powershell
dotnet run --project C:\Work\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path C:\Work\model-validator-challenges\<language>\<challenge-id>
```

## Generated Outputs

Do not commit benchmark runs, logs, temporary workspaces, generated verification JSON or candidate patches. Those files belong in ignored output directories such as `runs/`, `artifacts/`, `outputs/`, `temp/` or an external results location.
