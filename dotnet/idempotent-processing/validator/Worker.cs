using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using CommandProcessor;

Request request = JsonSerializer.Deserialize<Request>(Console.In.ReadToEnd())!;
AccountLedger ledger = new();
if (request.Concurrent)
{
    // Dedicated threads and a barrier avoid serial execution by a small thread pool.
    foreach (CommandMessage[] batch in request.Commands.Chunk(16))
    {
        using Barrier start = new(batch.Length + 1);
        Thread[] threads = batch.Select(command => new Thread(() => {
            start.SignalAndWait();
            ledger.Apply(command);
        })).ToArray();
        foreach (Thread thread in threads) thread.Start();
        start.SignalAndWait();
        foreach (Thread thread in threads) thread.Join();
    }
}
else
    foreach (CommandMessage command in request.Commands) ledger.Apply(command);
Console.Write(JsonSerializer.Serialize(new { Balance = ledger.GetBalance(request.Account), Ids = ledger.AppliedCommandIds.ToArray() }));
record Request(CommandMessage[] Commands, string Account, bool Concurrent);
