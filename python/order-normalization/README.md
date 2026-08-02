# Order Normalization

Language: Python

This challenge asks a target agent to normalize inbound order events.

The starter workspace assumes line items already contain integer cent values and does no normalization. A correct solution must parse decimal prices, combine duplicate SKUs, omit zero-quantity lines, sort normalized lines and avoid mutating the input event.

## Assertions

| Assertion | Type | What It Checks |
| --- | --- | --- |
| `duplicate-lines` | requirement | Duplicate SKU lines are normalized, sorted and combined |
| `decimal-cents` | requirement | Decimal prices are rounded half up into integer cents and totals are correct |
| `metadata-and-input-stability` | regression | `order_id` is preserved, `currency` is uppercased and the input event is not mutated |

## Verify

```powershell
dotnet run --project C:\Work\model-validator\src\ModelValidator.Cli\ModelValidator.Cli.csproj -c Release -- challenge verify --path C:\Work\model-validator-challenges\python\order-normalization
```
