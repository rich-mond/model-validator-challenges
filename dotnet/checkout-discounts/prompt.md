# Task

Implement deterministic checkout discount pricing in `src/Checkout`.

The pricing service must:

- calculate `SubtotalCents` from catalog prices and item quantities;
- apply customer tier discount before coupon discounts:
  - `Bronze`: 0%
  - `Silver`: 5%
  - `Gold`: 10%
- round percentage discounts to the nearest cent using midpoint-away-from-zero rounding;
- evaluate coupon codes case-insensitively and ignore duplicate coupon codes after their first occurrence;
- ignore unknown or inactive coupon codes;
- use the injected `IClock.UtcNow` for coupon validity, not wall-clock time;
- apply stackable coupons in first-seen input order after tier discount;
- if any active supplied coupon is non-stackable, apply only the best single active coupon and ignore all stackable coupons;
- cap coupon discounts by `MaxDiscountCents` when present;
- never reduce `TotalCents` below zero;
- return `DiscountLine` entries in the order discounts are applied.

Keep the public API usable by the existing tests. Do not remove or rename the public model types.
