# Order Normalization

Language: Python

Complexity: Foundation

This challenge asks a target agent to normalize inbound order events.

The starter workspace assumes line items already contain integer cent values and does no normalization. A correct solution must parse decimal prices, combine duplicate SKUs, omit zero-quantity lines, sort normalized lines and avoid mutating the input event.

## Assertions

| Assertion | Type | What It Checks |
| --- | --- | --- |
| `duplicate-lines` | requirement | Duplicate SKU lines are normalized, sorted and combined |
| `decimal-cents` | requirement | Decimal prices are rounded half up into integer cents and totals are correct |
| `metadata-and-input-stability` | regression | `order_id` is preserved, `currency` is uppercased and the input event is not mutated |

## Public Self-Check

The materialised starter workspace includes public unit tests. Target agents must run this exact command after editing:

```powershell
python -m unittest discover -s tests
```

This is a local sanity check. The hidden Docker validator remains the authoritative benchmark result.

## Verify

```powershell
dotnet run --project ..\..\..\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path .
```
