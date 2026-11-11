using System.Text.Json;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
string group = args.Single();
DateTimeOffset start = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
var orders = Enumerable.Range(0, 26).Select(i => new Fixture($"a-{i:00}", "tenant-one", start.AddDays(i / 3),
    new[] { "Pending", "Cancelled", "Dispatched" }[i % 3], 1000L + i)).Concat(
    Enumerable.Range(0, 3).Select(i => new Fixture($"b-{i:00}", "tenant-two", start.AddYears(1), "Pending", 9000L + i))).ToArray();
List<Call> calls = [];
List<Action<JsonElement>> checks = [];
string Date(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("O"));
void Page(string path, string tenant = "tenant-one", string? status = null,
    DateTimeOffset? from = null, DateTimeOffset? to = null, int page = 1, int size = 20, string? spoof = null)
{
    var expected = orders.Where(x => x.TenantId == tenant && (status is null || x.Status == status) &&
        (!from.HasValue || x.CreatedUtc >= from) && (!to.HasValue || x.CreatedUtc < to))
        .OrderByDescending(x => x.CreatedUtc).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
    var items = expected.Skip((int)Math.Min((long)(page - 1) * size, int.MaxValue)).Take(size).ToArray();
    calls.Add(new(path, tenant == "tenant-one" ? "tenant-one-token" : "tenant-two-token", spoof));
    checks.Add(response => {
        Require(response.GetProperty("Status").GetInt32() == 200, "Expected a successful search: " + path);
        var body = JsonDocument.Parse(response.GetProperty("Body").GetString()!).RootElement;
        Require(body.GetProperty("total").GetInt64() == expected.Length, "Matching count before pagination: " + path);
        Require(body.GetProperty("page").GetInt32() == page && body.GetProperty("pageSize").GetInt32() == size, "Pagination response contract.");
        var actual = body.GetProperty("items").EnumerateArray().ToArray();
        Require(actual.Length == items.Length, "Page length: " + path);
        for (int i = 0; i < actual.Length; i++)
        {
            Require(actual[i].GetProperty("id").GetString() == items[i].Id &&
                actual[i].GetProperty("createdUtc").GetDateTimeOffset() == items[i].CreatedUtc &&
                actual[i].GetProperty("status").GetString() == items[i].Status &&
                actual[i].GetProperty("totalMinor").GetInt64() == items[i].TotalMinor,
                "Ordered item/tenant/projection mismatch: " + path);
        }
    });
}
void Status(string path, int status, string? token = "tenant-one-token", string? spoof = null)
{
    calls.Add(new(path, token, spoof));
    checks.Add(response => Require(response.GetProperty("Status").GetInt32() == status, $"Expected {status}: {path}"));
}
void Single(string id, string tenant)
{
    var expected = orders.Single(x => x.Id == id && x.TenantId == tenant);
    calls.Add(new("/orders/" + id, tenant == "tenant-one" ? "tenant-one-token" : "tenant-two-token", null));
    checks.Add(response => {
        Require(response.GetProperty("Status").GetInt32() == 200, "Existing single-order retrieval.");
        var body = JsonDocument.Parse(response.GetProperty("Body").GetString()!).RootElement;
        Require(body.GetProperty("id").GetString() == id && body.GetProperty("totalMinor").GetInt64() == expected.TotalMinor &&
            body.GetProperty("status").GetString() == expected.Status, "Existing DTO fields.");
    });
}
if (group == "search-filters")
{
    Page("/orders");
    Page("/orders?status=Pending", status: "Pending");
    Page("/orders?status=cancelled", status: "Cancelled");
    Page("/orders?createdFrom=" + Date(start.AddDays(2)), from: start.AddDays(2));
    Page("/orders?createdTo=" + Date(start.AddDays(5)), to: start.AddDays(5));
    Page("/orders?status=Cancelled&createdFrom=" + Date(start.AddDays(2)) + "&createdTo=" + Date(start.AddDays(8)) + "&page=2&pageSize=2",
        status: "Cancelled", from: start.AddDays(2), to: start.AddDays(8), page: 2, size: 2);
    Page("/orders?createdFrom=" + Date(start) + "&createdTo=" + Date(start), from: start, to: start);
    Page("/orders?createdFrom=" + Date(start.AddYears(4)), from: start.AddYears(4));
}
else if (group == "pagination-and-ordering")
{
    Page("/orders");
    Page("/orders?page=2", page: 2);
    Page("/orders?page=3", page: 3);
    Page("/orders?pageSize=50", size: 50);
    Page("/orders?pageSize=1&page=2", page: 2, size: 1);
    Page("/orders?pageSize=3&page=2", page: 2, size: 3);
    Page("/orders?page=2147483647&pageSize=50", page: int.MaxValue, size: 50);
}
else if (group == "tenant-and-retrieval")
{
    Page("/orders?tenantId=tenant-two", spoof: "tenant-two");
    Page("/orders", tenant: "tenant-two");
    Status("/orders", 401, token: null, spoof: "tenant-two");
    Status("/orders", 401, token: "invalid");
    Single("a-00", "tenant-one");
    Single("b-00", "tenant-two");
    Status("/orders/b-00", 404);
    Status("/orders/missing", 404);
    Status("/orders/a-00", 401, token: null);
    var created = new { id = "made-a", createdUtc = start, status = "Pending", totalMinor = 765 };
    calls.Add(new("/orders", "tenant-one-token", null, "POST", created));
    checks.Add(response => Require(response.GetProperty("Status").GetInt32() == 201, "Existing order creation."));
    calls.Add(new("/orders", "tenant-one-token", null, "POST", created));
    checks.Add(response => Require(response.GetProperty("Status").GetInt32() == 409, "Duplicate order IDs."));
    calls.Add(new("/orders", "tenant-one-token", null, "POST", new { id = "bad", createdUtc = start, status = "Pending", totalMinor = -1 }));
    checks.Add(response => Require(response.GetProperty("Status").GetInt32() == 400, "Existing creation validation."));
    calls.Add(new("/orders/made-a", "tenant-one-token", null));
    checks.Add(response => {
        Require(response.GetProperty("Status").GetInt32() == 200, "Created order did not persist.");
        var body = JsonDocument.Parse(response.GetProperty("Body").GetString()!).RootElement;
        Require(body.GetProperty("totalMinor").GetInt64() == 765, "Created total changed.");
    });
    Status("/orders/made-a", 404, token: "tenant-two-token");
}
else if (group == "invalid-requests")
{
    foreach (string query in new[] { "page=0", "page=-1", "page=abc", "page=2147483648", "pageSize=0", "pageSize=-1",
        "pageSize=51", "pageSize=1.5", "status=Returned", "status=1", "createdFrom=garbage", "createdTo=2026-02-30T00:00:00Z",
        "createdFrom=2026-02-01", "createdFrom=2026-02-01T00:00:00%2B02:00" }) Status("/orders?" + query, 400);
    Status("/orders?createdFrom=" + Date(start.AddDays(3)) + "&createdTo=" + Date(start), 400);
}
else throw new ArgumentException("Unknown group.");
var result = CandidateProbe.Run<JsonElement>(new { Orders = orders, Calls = calls });
var responses = result.GetProperty("Responses").EnumerateArray().ToArray();
Require(responses.Length == checks.Count && checks.Count != 0, "Every HTTP check must complete.");
for (int i = 0; i < responses.Length; i++) checks[i](responses[i]);
Console.WriteLine("PASS " + group);

record Fixture(string Id, string TenantId, DateTimeOffset CreatedUtc, string Status, long TotalMinor);
record Call(string Path, string? Token, string? SpoofTenant, string Method = "GET", object? Body = null);
