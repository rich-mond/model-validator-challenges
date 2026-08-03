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
| `clock-and-source-boundary` | requirement | Coupon validity uses the injected clock and candidate source does not call wall-clock APIs directly |
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
