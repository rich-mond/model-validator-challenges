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

| Pack | Language | Complexity | Task | Verification |
| --- | --- | --- | --- | --- |
| `dotnet/idempotent-processing` | C# / .NET | Foundation | Make command processing idempotent under duplicate and concurrent delivery | Docker validator checks duplicate delivery, concurrent delivery and distinct-command regression |
| `dotnet/checkout-discounts` | C# / .NET | Intermediate | Implement deterministic checkout pricing with tier discounts, coupon stacking and clock-bound validity | Docker validator checks tier/coupon behavior, non-stackable choice, injected-clock boundaries and cap/floor regressions |
| `python/order-normalization` | Python | Foundation | Normalize inbound order events without mutating input | Docker validator checks duplicate SKU aggregation, decimal cent rounding and metadata/input stability |
| `dotnet/order-lifecycle-regression` | C# / .NET | Intermediate | Investigate overlapping cancellation/dispatch requests in an existing service | Real SQLite state/audit transitions and deterministic overlap |
| `dotnet/order-search` | C# / .NET | Intermediate | Implement search across API, service and persistence layers | Real HTTP, authentication, tenant isolation, filters and pagination |
| `dotnet/customer-address-migration` | C# / .NET | Intermediate | Upgrade populated databases while preserving legacy customer responses | Real SQLite migration, address invariants and rollback |

Each listed pack is expected to pass `modelval challenge verify` before it is used in a benchmark.

Complexity is separate from language. A language can have foundation, intermediate and advanced packs side by side. Foundation challenges are useful for smoke testing the workflow and basic model behavior; higher-complexity packs should introduce more files, more plausible wrong choices and stronger hidden validation.

See [Challenge Complexity](docs/challenge-complexity.md) for the classification rubric.

## Fresh Start

Clone the framework and challenge repos side by side:

```powershell
$root = Join-Path $HOME "model-validator-work"
New-Item -ItemType Directory -Force -Path $root | Out-Null
Set-Location $root
git clone https://github.com/rich-mond/model-validator.git
git clone https://github.com/rich-mond/model-validator-challenges.git
```

Build the framework:

```powershell
Set-Location .\model-validator
dotnet restore ModelValidator.slnx --locked-mode
dotnet build ModelValidator.slnx -c Release
dotnet test ModelValidator.slnx -c Release --no-build
```

Verify all six supported packs:

```powershell
dotnet run --project .\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..\model-validator-challenges\dotnet\idempotent-processing
dotnet run --project .\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..\model-validator-challenges\dotnet\checkout-discounts
dotnet run --project .\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..\model-validator-challenges\python\order-normalization

dotnet run --project .\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..\model-validator-challenges\dotnet\order-lifecycle-regression

dotnet run --project .\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..\model-validator-challenges\dotnet\order-search

dotnet run --project .\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path ..\model-validator-challenges\dotnet\customer-address-migration
```

Verification proves a challenge pack is internally coherent before any model or agent is benchmarked. The starter workspace must fail required assertions, the oracle patch must pass, repeated oracle validation must be stable and known-invalid patches must fail.

Successful verification prints the checks it performed and a short summary: manifest and digest checks, validator calibration, oracle stability and counterexample count.

## Pack Anatomy

Each challenge pack contains a manifest, prompt, starter Git bundle, hidden validator, oracle patch and counterexamples. The full file-format contract lives in [Challenge Authoring](docs/challenge-authoring.md).

## What The Target Agent Sees

During a benchmark, the target agent receives only:

- the materialised starter workspace;
- the task prompt;
- public self-check commands from the challenge manifest;
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

Most users do not write target JSON or adapter scripts. From the framework repo, run `benchmark` with the challenge path. Interactive mode lets you choose the model from VS Code or another UI.

```powershell
Set-Location .\model-validator
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- benchmark --challenge ..\model-validator-challenges\python\order-normalization --open vscode --output ..\model-validator-runs\order-normalization-vscode
```

For the .NET challenge:

```powershell
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- benchmark --challenge ..\model-validator-challenges\dotnet\idempotent-processing --open vscode --output ..\model-validator-runs\idempotent-processing-vscode
```

The framework creates the candidate workspace, writes `AGENTS.md` and `MODEL_VALIDATOR_TASK.md` into that workspace, opens VS Code when requested, then waits. Use your chosen model or coding-agent UI against the printed workspace and send exactly: `read AGENTS.md and follow it`. The generated `AGENTS.md` contains the task boundary and test requirements. The task file includes the challenge's exact public self-check command. The agent must run that command after editing, must not substitute another check and must report the command with its exit code. Return to the terminal and press Enter only after the model reports those self-check results. Validation starts after that.

If VS Code cannot be opened automatically, open the printed workspace and task file manually. The benchmark will still continue when you press Enter.

After validation, the framework captures the candidate patch, runs this pack's hidden validator, writes detailed reports under the output directory and prints a console-friendly score summary. The generated `AGENTS.md` and `MODEL_VALIDATOR_TASK.md` files are excluded from candidate scoring.

To print the console score summary again later:

```powershell
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- results --run ..\model-validator-runs\order-normalization-vscode
```

To print the persisted Markdown or JSON reports instead, use `report --run <run-directory> --format markdown` or `report --run <run-directory> --format json`.

For another agent CLI, pass the command after `--`:

```powershell
dotnet run --project src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- benchmark --challenge ..\model-validator-challenges\python\order-normalization --agent codex-cli --provider openai --model gpt-5 --output ..\model-validator-runs\codex-cli-order-normalization -- codex -a never exec --model "{model}" --sandbox workspace-write "{prompt}"
```

Supported placeholders are `{prompt}`, `{promptPath}`, `{workspace}`, `{output}` and `{model}`.

## Advanced Plan-Based Use

For scripted comparisons, a benchmark plan can point at one challenge directory and one or more target configuration files:

```json
{
  "schemaVersion": "1.0",
  "planId": "first-order-normalization-run",
  "challengePath": "<challenge-repo>\\python\\order-normalization",
  "targets": [
    "<targets>\\codex-gpt-5-default.json"
  ],
  "attemptsPerTarget": 1,
  "outputPath": "<runs>\\first-order-normalization-run",
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
dotnet run --project ..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .\<language>\<challenge-id>
```

## Generated Outputs

Do not commit benchmark runs, logs, temporary workspaces, generated verification JSON or candidate patches. Those files belong in ignored output directories such as `runs/`, `artifacts/`, `outputs/`, `temp/` or an external results location.

## Reproducible qualification

The CI workflow qualifies all six packs against an exact framework commit. It records the framework and challenge commits and uploads qualification logs and results even on failure. To update compatibility, qualify all six packs against the proposed framework commit, then change the workflow's framework `ref` to that full commit SHA in a reviewed PR. A moving branch is not a qualified revision. Reproduce locally by checking out the recorded pair and running the six `challenge verify` commands above. Unexpected non-ignored files or tracked modifications fail CI hygiene.

The 1.0.1 pack revisions strengthen validators and calibration without changing starter bundles or oracle solutions. Candidate calls run in child processes; trusted comparisons run in the supervisor. This rejects the demonstrated early exits, but is not protection against arbitrary malicious interference inside the shared validator container.

Python order normalization 1.1.0 explicitly retains the first non-zero-quantity line's unit price when duplicate normalized SKUs have different prices. Its prompt, public starter test and validator agree; later prices and discarded zero-quantity lines cannot replace that price. The existing oracle is unchanged.

## Local Codex smoke runs

Qualify a pack before evaluating a real candidate. Inspect your installed CLI with `codex --version`, `codex exec --help` and `codex login status`. Use an actually configured model; do not infer a model version from its name. Do not install Codex or change credentials merely to complete a smoke run.

From the framework repository, repeat this command individually for `order-lifecycle-regression`, `order-search` and `customer-address-migration`, changing `$pack` and selecting a fresh output path each time:

```powershell
$pack = "order-lifecycle-regression"
$model = "<model actually configured in your Codex installation>"
$codex = (Get-Command codex -CommandType Application).Source
dotnet run --project src/ModelValidator.Cli/ModelValidator.Cli.csproj -c Release -- benchmark --challenge "../model-validator-challenges/dotnet/$pack" --agent codex-cli --agent-version "<version from codex --version>" --provider openai --model $model --attempts 1 --output "../model-validator-runs/$pack-codex" -- $codex -a never exec --ignore-user-config --ephemeral --model "{model}" --sandbox workspace-write --cd "{workspace}" --json "{prompt}"
```

These flags are supported by the locally inspected CLI (0.167.0-alpha.4.1); inspect help for your installation rather than assuming flags. A native executable is required by the process adapter. The new invocation is fresh and receives the materialised public task, not a resumed author session or oracle patch. It must run the public self-check before finishing. Reports and patches are retained outside both repositories; unavailable usage stays unknown.

A host process adapter is not a filesystem isolation boundary: workspace-write does not establish that hidden author files are unreadable. Record these attempts as non-authoritative local smoke tests. Author-initiated runs are not independent evidence of model performance, even when successful. Report calibration, actual agent outcome, public checks, hidden assertions, timing and infrastructure failures separately. A failed candidate is not permission to weaken a legitimate assertion. Codex model calls are never part of CI.
