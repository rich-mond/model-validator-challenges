using System;
using System.Collections.Generic;
using System.Text.Json;
using Checkout;
Request request = JsonSerializer.Deserialize<Request>(Console.In.ReadToEnd())!;
CheckoutPricingService service = new(new FixedClock(request.Now), new Dictionary<string, int>(request.Catalog, StringComparer.OrdinalIgnoreCase), request.Rules);
Console.Write(JsonSerializer.Serialize(service.Quote(request.Customer, request.Items, request.CouponCodes)));
record Request(DateTimeOffset Now, Dictionary<string, int> Catalog, DiscountRule[] Rules, CustomerProfile Customer, CartItem[] Items, string[] CouponCodes);
