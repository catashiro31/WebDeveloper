# Notifications regression

Run against a migrated **test** PostgreSQL database only:

```
NOTIFICATION_TEST_CONNECTION='Host=127.0.0.1;Database=Docbooking;Username=postgres' dotnet run --project tests/NotificationRegression
```

The test creates users, doctor profile, appointment, medical result and transfer in a
transaction and rolls everything back. It checks recipient routing, no-op save
deduplication, per-user read/read-all, idempotent marking, and atomic rollback when a
business write fails. It invokes the actual notification controllers for ownership
checks; HTTP authentication and browser interaction are tested separately.
