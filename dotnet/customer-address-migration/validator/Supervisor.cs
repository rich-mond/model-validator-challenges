using System.Text.Json;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static void Address(JsonElement value, string line, string city, string postal, string country)
{
    Require(value.GetProperty("Line1").GetString() == line && value.GetProperty("City").GetString() == city &&
        value.GetProperty("PostalCode").GetString() == postal && value.GetProperty("CountryCode").GetString() == country, "Address fields were lost or selected incorrectly.");
}
string group = args.Single();
var result = CandidateProbe.Run<JsonElement>(new { Group = group });
if (group == "transactional-upgrade")
{
    Require(result.GetProperty("Failed").GetBoolean(), "Fixture must interrupt the version-two migration.");
    Require(result.GetProperty("Version").GetInt64() == 1, "Version was committed after failure.");
    Require(result.GetProperty("BeforeSchema").GetRawText() == result.GetProperty("AfterSchema").GetRawText(), "Schema changes were not rolled back.");
    Require(result.GetProperty("BeforeData").GetRawText() == result.GetProperty("AfterData").GetRawText(), "Customer data changes were not rolled back.");
    Console.WriteLine("PASS " + group);
    return;
}
Require(result.GetProperty("Version").GetInt64() == 2, "The existing migration mechanism must record version two.");
var data = result.GetProperty("Data");
var rows = result.GetProperty("Rows").EnumerateArray().ToArray();
var customers = result.GetProperty("Customers").EnumerateArray().ToArray();
if (group != "fresh-database")
{
    Require(customers.Select(x => x.GetProperty("Id").GetString()).SequenceEqual(new[] { "c-1", "c-2", "c-3" }), "Existing customer IDs lost.");
    Require(customers.Select(x => x.GetProperty("Name").GetString()).SequenceEqual(new[] { "Ada", "Grace, Hopper", "Lin" }), "Existing customer names lost.");
}
foreach (var customer in rows.GroupBy(x => x.GetProperty("CustomerId").GetString()))
{
    Require(customer.Count(x => x.GetProperty("IsPrimary").GetInt64() == 1) == 1, "Persisted addresses must have exactly one primary.");
}
Require(rows.Select(x => x.GetProperty("Id").GetString()).All(x => !string.IsNullOrWhiteSpace(x)) &&
    rows.Select(x => x.GetProperty("Id").GetString()).Distinct().Count() == rows.Length, "Address IDs must be unique and nonempty.");
if (group == "populated-upgrade")
{
    Require(rows.Length == 2, "Original addresses were not migrated exactly once.");
    Address(rows.Single(x => x.GetProperty("CustomerId").GetString() == "c-1"), "12 Bridge St", "Bath", "BA1 1AA", "GB");
    Address(rows.Single(x => x.GetProperty("CustomerId").GetString() == "c-2"), "4 Rue de l'Ete", "Lyon", "69001", "FR");
    var views = data.GetProperty("Views").EnumerateArray().ToArray();
    Address(views[0].GetProperty("Address"), "12 Bridge St", "Bath", "BA1 1AA", "GB");
    Address(views[1].GetProperty("Address"), "4 Rue de l'Ete", "Lyon", "69001", "FR");
    Require(views[2].GetProperty("Address").ValueKind == JsonValueKind.Null, "Addressless customer changed.");
    var lists = data.GetProperty("Lists").EnumerateArray().ToArray();
    Require(lists.Select(x => x.GetArrayLength()).SequenceEqual(new[] { 1, 1, 0 }), "Address retrieval contract.");
    Require(lists[0][0].GetProperty("IsPrimary").GetBoolean() && lists[1][0].GetProperty("IsPrimary").GetBoolean(), "Migrated addresses are primary.");
}
else if (group == "address-operations")
{
    Require(data.GetProperty("Selected").GetBoolean() && data.GetProperty("Repeated").GetBoolean() && data.GetProperty("Updated").GetBoolean(), "Primary/update operations failed.");
    string id = data.GetProperty("SelectedId").GetString()!;
    Address(data.GetProperty("SelectedView").GetProperty("Address"), "22 Oak Rd", "York", "YO1", "GB");
    Address(data.GetProperty("UpdatedView").GetProperty("Address"), "24 Oak Rd", "York", "YO2", "GB");
    Require(data.GetProperty("Primary").GetProperty("Id").GetString() == id, "Primary getter mismatch.");
    var own = rows.Where(x => x.GetProperty("CustomerId").GetString() == "c-1").ToArray();
    Require(own.Length == 2 && data.GetProperty("Addresses").GetArrayLength() == 2, "Multiple addresses not persisted.");
    Address(own.Single(x => x.GetProperty("Id").GetString() == id), "24 Oak Rd", "York", "YO2", "GB");
    Address(own.Single(x => x.GetProperty("Id").GetString() != id), "12 Bridge St", "Bath", "BA1 1AA", "GB");
}
else if (group == "primary-invariants")
{
    Require(data.GetProperty("InitiallyEmpty").GetArrayLength() == 0 && data.GetProperty("InitiallyPrimary").ValueKind == JsonValueKind.Null, "Customer without addresses.");
    Require(data.GetProperty("First").GetProperty("IsPrimary").GetBoolean(), "First address must become primary.");
    Require(!data.GetProperty("Second").GetProperty("IsPrimary").GetBoolean(), "Second address should preserve the primary.");
    foreach (var stage in new[] { (Key: "FirstRows", Count: 1), (Key: "SecondRows", Count: 2) })
    {
        var persisted = data.GetProperty(stage.Key).EnumerateArray().Where(x => x.GetProperty("CustomerId").GetString() == "c-3").ToArray();
        Require(persisted.Length == stage.Count && persisted.Count(x => x.GetProperty("IsPrimary").GetInt64() == 1) == 1, "Intermediate primary state was not persisted correctly.");
    }
    Require(data.GetProperty("Third").GetProperty("IsPrimary").GetBoolean(), "Explicit primary addition.");
    Require(!data.GetProperty("ForeignSelection").GetBoolean() && !data.GetProperty("MissingSelection").GetBoolean() &&
        data.GetProperty("UnknownRejected").GetBoolean(), "Invalid selections must be rejected without damaging state.");
    Require(data.GetProperty("Primary").GetProperty("Id").GetString() == data.GetProperty("Third").GetProperty("Id").GetString(), "Invalid selection changed the primary.");
    Require(rows.Count(x => x.GetProperty("CustomerId").GetString() == "c-3") == 3 && data.GetProperty("Addresses").GetArrayLength() == 3, "Address count/ownership.");
    Address(data.GetProperty("Legacy").GetProperty("Address"), "Three", "Bath", "P3", "GB");
}
else if (group == "fresh-database")
{
    Require(customers.Length == 2 && rows.Length == 2 && data.GetProperty("Count").GetInt64() == 2, "Fresh schema creation/customer directory.");
    Require(data.GetProperty("Renamed").GetBoolean() && data.GetProperty("Updated").GetBoolean(), "Existing update APIs.");
    Require(data.GetProperty("View").GetProperty("Name").GetString() == "Fresh revised", "Name update did not persist.");
    Address(data.GetProperty("View").GetProperty("Address"), "Revised", "Leeds", "LS2", "GB");
    Require(data.GetProperty("EmptyView").GetProperty("Address").ValueKind == JsonValueKind.Null &&
        data.GetProperty("EmptyAddresses").GetArrayLength() == 0 && data.GetProperty("MissingView").ValueKind == JsonValueKind.Null, "Existing null/unknown contract.");
    Require(data.GetProperty("Addresses").GetArrayLength() == 1 && data.GetProperty("Primary").GetProperty("IsPrimary").GetBoolean(), "Fresh primary relationship.");
    Require(data.GetProperty("Established").GetBoolean(), "Existing SetAddress must establish a first address.");
    Address(data.GetProperty("EstablishedView").GetProperty("Address"), "Established", "Leeds", "LS3", "GB");
    Require(rows.Count(x => x.GetProperty("CustomerId").GetString() == "new-2") == 1, "New first address was not persisted.");
    Require(data.GetProperty("Page").GetArrayLength() == 1 && data.GetProperty("Csv").GetString()!.Contains("Revised", StringComparison.Ordinal), "Directory/export regression.");
}
else throw new ArgumentException("Unknown group.");
Console.WriteLine("PASS " + group);
