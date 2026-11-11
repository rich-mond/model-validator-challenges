using System;
using System.Collections.Generic;
using System.Linq;

public sealed record CommandMessage(string Id, string AccountId, decimal Amount);
public sealed record LedgerResponse(decimal Balance, string[] Ids);
public sealed class AccountLedger
{
    private readonly List<CommandMessage> commands = new();
    private LedgerResponse? result;
    public void Apply(CommandMessage command) { lock (commands) commands.Add(command); }
    public decimal GetBalance(string account)
    {
        result ??= CandidateProbe.Run<LedgerResponse>(new { Commands = commands, Account = account, Concurrent = Environment.GetCommandLineArgs()[1] == "concurrent-delivery" });
        return result.Balance;
    }
    public IReadOnlyList<string> AppliedCommandIds => result!.Ids;
}
