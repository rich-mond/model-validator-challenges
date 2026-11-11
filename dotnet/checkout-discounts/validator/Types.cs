using System;
using System.Collections.Generic;
public enum DiscountKind { FixedAmount, Percent }
public enum CustomerTier { Bronze, Silver, Gold }
public sealed record CartItem(string Sku, int Quantity);
public sealed record CustomerProfile(string CustomerId, CustomerTier Tier);
public sealed record DiscountLine(string Code, int AmountCents);
public sealed record PriceQuote(int SubtotalCents, int TierDiscountCents, IReadOnlyList<DiscountLine> Discounts, int TotalCents);
public sealed record DiscountRule(string Code, DiscountKind Kind, int Amount, bool Stackable, DateTimeOffset StartsUtc, DateTimeOffset EndsUtc, int? MaxDiscountCents);
public sealed record FixedClock(DateTimeOffset UtcNow);
public sealed class CheckoutPricingService(FixedClock clock, IReadOnlyDictionary<string, int> catalog, IEnumerable<DiscountRule> rules)
{
    public PriceQuote Quote(CustomerProfile customer, IReadOnlyList<CartItem> items, IReadOnlyList<string> couponCodes) =>
        CandidateProbe.Run<PriceQuote>(new { Now = clock.UtcNow, Catalog = catalog, Rules = rules, Customer = customer, Items = items, CouponCodes = couponCodes });
}
