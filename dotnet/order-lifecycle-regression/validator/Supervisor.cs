using System.Text.Json;

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
string group = args.Single();
JsonElement result = CandidateProbe.Run<JsonElement>(new { Group = group });
var states = result.GetProperty("States").EnumerateArray().ToDictionary(x => x.GetProperty("Id").GetString()!);
var events = result.GetProperty("Audits").EnumerateArray().ToArray();
bool[] outcomes = result.GetProperty("Outcomes").EnumerateArray().Select(x => x.GetBoolean()).ToArray();
Require(states.Count == 3 && states["a"].GetProperty("TotalMinor").GetInt64() == 1234 &&
    states["b"].GetProperty("TotalMinor").GetInt64() == 999, "Order identity/details were lost.");
Require(states["c"].GetProperty("Status").GetString() == "Pending", "Unrelated pending order changed.");
if (group is "ordinary-transitions" or "retry-and-guard")
{
    bool[] expected = group == "ordinary-transitions" ? [true,true,false] : [true,true,false,true,true,false];
    Require(outcomes.SequenceEqual(expected), "Transition/retry result contract.");
    Require(states["a"].GetProperty("Status").GetString() == "Cancelled", "Cancellation did not persist.");
    Require(states["b"].GetProperty("Status").GetString() == "Dispatched", "Dispatch did not persist.");
    Require(events.Length == 2 && events.Count(x => x.GetProperty("OrderId").GetString() == "a" && x.GetProperty("Status").GetString() == "Cancelled") == 1 &&
        events.Count(x => x.GetProperty("OrderId").GetString() == "b" && x.GetProperty("Status").GetString() == "Dispatched") == 1,
        "Exactly one matching audit per terminal transition.");
}
else if (group == "concurrent-transition")
{
    Require(outcomes.Length == 4, "Both request pairs must complete.");
    foreach (var test in new[] { (Id: "a", Offset: 0), (Id: "b", Offset: 2) })
    {
        bool cancelled = outcomes[test.Offset];
        bool dispatched = outcomes[test.Offset + 1];
        Require(cancelled != dispatched, "Exactly one competing transition must win.");
        string expected = cancelled ? "Cancelled" : "Dispatched";
        Require(states[test.Id].GetProperty("Status").GetString() == expected, "Loser overwrote persisted winner.");
        var audits = events.Where(x => x.GetProperty("OrderId").GetString() == test.Id).ToArray();
        Require(audits.Length == 1 && audits[0].GetProperty("Status").GetString() == expected,
            "Loser or retry emitted a successful transition audit.");
    }
}
else if (group == "existing-behaviour")
{
    Require(outcomes.Length == 4 && outcomes.All(x => x), "Existing creation/lookup/rename behaviour.");
    Require(states["a"].GetProperty("Customer").GetString() == "Ada revised", "Rename did not persist.");
    Require(result.GetProperty("DuplicateRejected").GetBoolean() && result.GetProperty("NegativeRejected").GetBoolean(), "Existing input validation.");
    Require(result.GetProperty("Pending").EnumerateArray().Select(x => x.GetString()).SequenceEqual(new[] { "b", "c" }), "Pending work list.");
    string receipt = result.GetProperty("Receipt").GetString()!;
    Require(receipt.Contains("12.34", StringComparison.Ordinal) && receipt.Contains("Ada revised", StringComparison.Ordinal), "Existing receipt projection.");
}
else throw new ArgumentException("Unknown group.");
Console.WriteLine("PASS " + group);
