# Task

Make command processing idempotent under duplicate and concurrent delivery.

The ledger must apply each command ID at most once, preserve normal processing for distinct commands, and remain safe when the same command is delivered concurrently.
