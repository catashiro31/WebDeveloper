# Workflow regression

Use an isolated PostgreSQL database, never production. The runner applies migrations
and writes unique fixtures; it does not remove them. Use a fresh database per run
because the email processor also consumes existing pending outbox rows.

```bash
WORKFLOW_TEST_CONNECTION='Host=127.0.0.1;Database=workflow_test;Username=postgres' dotnet run --project tests/WorkflowRegression
```

Requires .NET 10 and PostgreSQL. SMTP is replaced by a fake sender; no real email
is sent. Checks cover atomic signup/email queueing, delivery failure and retry,
resend cooldown, stale verification discard, seven-day scheduling lead time,
complete 14-day schedules and idempotency, blocking early results, degree
validation, approval without a reason, required rejection reason, decision email
deduplication, notification ownership, unread preservation and pending-tab links.

HTTP authentication and rendered browser behavior require separate UI checks.
