# Checkout Discounts

Language: C# / .NET

Complexity: Intermediate

This challenge asks a target agent to implement deterministic checkout pricing across a small multi-file domain model.

The starter workspace has catalog pricing, customer tiers, coupon rules and a pricing service, but the service takes shortcuts: it ignores tier discounts, uses wall-clock time directly, applies only the first coupon and does not handle non-stackable coupon choice correctly.

## Assertions

| Assertion | Type | What It Checks |
| --- | --- | --- |
| `tier-and-stackable-coupons` | requirement | Tier discounts are applied before stackable coupons, with deterministic rounding and discount lines |
| `non-stackable-best-coupon` | requirement | A non-stackable active coupon wins by best discount and suppresses stackable coupons |
| `clock-and-source-boundary` | requirement | Coupon validity follows injected-clock boundaries, including instants far from system time |
| `dedupe-cap-and-floor` | regression | Duplicate coupon codes, max caps and zero-floor totals are handled without breaking quote shape |

## Public Self-Check

The materialised starter workspace includes a public test project. Target agents must run this exact command after editing:

```powershell
dotnet test src\Checkout.Tests\Checkout.Tests.csproj
```

This is a local sanity check. The hidden Docker validator remains the authoritative benchmark result.

## Verify

```powershell
dotnet run --project ..\..\..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .
```

## Validator reliability (1.0.1)

Clock checks cover inclusive start/end instants, adjacent ticks and distant injected dates. Rounding, caps and zero-floor checks use concrete expected quotes. Harmless comments containing wall-clock API names are accepted: there is no source grep. Behavioural tests cannot prove that no wall-clock API is ever accessed.

Candidate calls run in a worker process and the supervisor compares returned data. Calibration includes successful early termination; Python also includes immediate process termination. Shared-container interference is outside this protection. Starter bundles and oracle patches are unchanged.
