using System.Reflection;
using System.Text.Json;
using OrderLifecycle;

var output = Console.Out;
Console.SetOut(Console.Error);
var request = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
string group = request.GetProperty("Group").GetString()!;
string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
var database = new OrderDatabase(path);
database.Initialize();
var repository = new OrderRepository(database);
var service = new OrderService(repository);
service.Create("a", "Ada", 1234);
service.Create("b", "Grace", 999);
service.Create("c", "Lin", 50);
List<bool> outcomes = [];
string? receipt = null;
bool duplicateRejected = false, negativeRejected = false;
if (group == "ordinary-transitions")
{
    outcomes.Add(await service.CancelAsync("a"));
    outcomes.Add(await service.DispatchAsync("b"));
    outcomes.Add(await service.CancelAsync("absent"));
}
else if (group == "retry-and-guard")
{
    outcomes.Add(await service.CancelAsync("a"));
    outcomes.Add(await service.CancelAsync("a"));
    outcomes.Add(await service.DispatchAsync("a"));
    outcomes.Add(await service.DispatchAsync("b"));
    outcomes.Add(await service.DispatchAsync("b"));
    outcomes.Add(await service.CancelAsync("b"));
}
else if (group == "concurrent-transition")
{
    foreach (bool separate in new[] { false, true })
    {
        var gated = DispatchProxy.Create<IOrderRepository, ReadGate>();
        var gate = (ReadGate)(object)gated;
        gate.Target = repository;
        var first = new OrderService(gated);
        var second = separate ? new OrderService(gated) : first;
        string id = separate ? "b" : "a";
        // Each call reaches its first incomplete await before returning. Both
        // pending snapshots are held for the defective implementation. A
        // serializing implementation need not perform two reads before release.
        Task<bool> cancel = first.CancelAsync(id);
        Task<bool> dispatch = second.DispatchAsync(id);
        gate.Release.TrySetResult();
        outcomes.AddRange(await Task.WhenAll(cancel, dispatch));
    }
}
else if (group == "existing-behaviour")
{
    var administration = new OrderAdministration(database);
    outcomes.Add(administration.RenameCustomer("a", "Ada revised"));
    outcomes.Add(!administration.RenameCustomer("absent", "Nobody"));
    outcomes.Add(await service.CancelAsync("a"));
    try { service.Create("a", "Duplicate", 10); }
    catch (Microsoft.Data.Sqlite.SqliteException) { duplicateRejected = true; }
    try { service.Create("negative", "Bad", -1); }
    catch (ArgumentOutOfRangeException) { negativeRejected = true; }
    var summary = await new OrderQueries(repository, new AuditLog(database)).GetSummaryAsync("a");
    receipt = OrderReceipt.Render(summary!);
    outcomes.Add(await new OrderQueries(repository, new AuditLog(database)).GetSummaryAsync("absent") is null);
}
else throw new ArgumentException("Unknown group.");

// Read the actual persisted rows using a new connection, not a cached service projection.
using var connection = database.Open();
using var orders = connection.CreateCommand();
orders.CommandText = "SELECT id,customer,total_minor,status FROM orders ORDER BY id";
List<object> states = [];
using (var reader = orders.ExecuteReader())
{
    while (reader.Read()) states.Add(new { Id = reader.GetString(0), Customer = reader.GetString(1), TotalMinor = reader.GetInt64(2), Status = reader.GetString(3) });
}
using var audit = connection.CreateCommand();
audit.CommandText = "SELECT order_id,status FROM transition_audit ORDER BY sequence";
List<object> events = [];
using (var reader = audit.ExecuteReader())
{
    while (reader.Read()) events.Add(new { OrderId = reader.GetString(0), Status = reader.GetString(1) });
}
output.Write(JsonSerializer.Serialize(new { Outcomes = outcomes, States = states, Audits = events,
    Pending = new OrderAdministration(database).PendingOrderIds(), Receipt = receipt, DuplicateRejected = duplicateRejected, NegativeRejected = negativeRejected }));

public class ReadGate : DispatchProxy
{
    public IOrderRepository Target { get; set; } = null!;
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == nameof(IOrderRepository.FindAsync))
        {
            var snapshot = ((Task<Order?>)targetMethod.Invoke(Target, args)!).GetAwaiter().GetResult();
            return Hold(snapshot);
        }
        return targetMethod.Invoke(Target, args);
    }

    private async Task<Order?> Hold(Order? snapshot)
    {
        await Release.Task;
        return snapshot;
    }
}
