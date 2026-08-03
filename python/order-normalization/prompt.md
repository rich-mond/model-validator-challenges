# Task

Implement `normalize_order(event)` in `src/order_normalizer.py`.

The function receives an order event dictionary:

- `order_id`: string
- `currency`: string
- `lines`: list of dictionaries with `sku`, `quantity` and `unit_price`

Return a new normalized dictionary with:

- the original `order_id`
- uppercase `currency`
- `lines` sorted by normalized SKU, where normalized SKU means surrounding whitespace trimmed and remaining text uppercased
- duplicate normalized SKU lines combined by summing quantities
- zero-quantity lines omitted
- `unit_price_cents` as an integer number of cents, rounded half up from `unit_price`
- `total_cents` equal to the sum of `quantity * unit_price_cents`

Do not mutate the input event.
