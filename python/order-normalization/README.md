# Order Normalization

Language: Python

Complexity: Foundation

This challenge asks a target agent to normalize inbound order events.

The starter workspace assumes line items already contain integer cent values and does no normalization. A correct solution must parse decimal prices, combine duplicate SKUs, omit zero-quantity lines, sort normalized lines and avoid mutating the input event.

## Assertions

| Assertion | Type | What It Checks |
| --- | --- | --- |
| `duplicate-lines` | requirement | Duplicate SKU lines are normalized, sorted and combined using the first non-zero line's price, with correct totals |
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

## Validator reliability (1.0.1)

The duplicate-lines assertion checks normalized quantities, unit cents, ordering and total. Separate checks cover half-up rounding and input mutation. The original equal-price duplicate scenarios remain covered.

Candidate calls run in a worker process and the supervisor compares returned data. Calibration includes successful early termination; Python also includes immediate process termination. Shared-container interference is outside this protection. Version 1.0.1 left starter bundles and oracle patches unchanged.

## Duplicate price contract (1.1.0)

For each normalized SKU, retain the unit price from the first non-zero-quantity line in original input order, convert it to cents with half-up rounding, and use that price for the entire combined quantity. Ignore later prices. Zero-quantity lines neither contribute quantity nor establish the price.

For example, two units at `1.005` followed by three units at `8.00` normalize to five units at 101 cents, totaling 505 cents. Reversing the price order uses 800 cents and totals 4000 cents. This deterministic rule preserves the existing oracle's behaviour; it does not calculate a weighted average or retain the last price.

The prompt, public starter tests and hidden duplicate-lines assertion all state or exercise this rule, including a leading zero-quantity line with a conflicting price. Targeted counterexamples reject last-price selection and price selection from discarded zero-quantity lines. Version 1.1.0 updates the starter bundle only to add the public test; candidate implementation and oracle patch are unchanged. Previous challenge fingerprints differ because the specification and declared inputs changed.
