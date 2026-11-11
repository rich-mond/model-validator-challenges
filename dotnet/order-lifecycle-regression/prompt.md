# Order lifecycle incident

The existing order application occasionally reports inconsistent terminal states
and transition audits when cancellation and dispatch requests overlap.
Investigate and fix the defect while preserving existing public behaviour.

- Orders begin Pending. Only Pending may become Cancelled or Dispatched.
- Cancellation cannot reverse dispatch, and dispatch cannot reverse cancellation.
- Exactly one of competing cancellation/dispatch requests may win. Either winner
  is valid. The loser must not overwrite the persisted winner or emit a
  successful transition audit.
- A successful terminal transition has exactly one matching persisted audit.
  Retrying the same completed operation succeeds without another audit.
- CancelAsync/DispatchAsync return true when that requested terminal state is
  established (including its retry), false for unknown orders or the opposing
  terminal state. Preserve these APIs and the existing repository boundary.
- Requests can use the same service instance or separate instances against the
  same SQLite database. State and audit must agree after reopening it.
- Preserve creation, order details, validation, summaries, pending-work queries,
  customer rename and receipt behaviour.

Use the existing .NET 10 code and SQLite dependency. Keep the public APIs usable
and add regression checks as appropriate. Do not replace persistence with an
in-memory implementation, change package versions, or redesign the application.
