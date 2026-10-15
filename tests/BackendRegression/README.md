# Backend regression

Run against an **isolated PostgreSQL test database**. The runner applies all migrations and writes generated test fixtures; do not point it at production. A fresh database is recommended for each run.

```sh
BACKEND_TEST_CONNECTION='Host=127.0.0.1;Database=backend_regression;Username=postgres' dotnet run --project tests/BackendRegression
```

Covers Vietnam-time schedule boundaries, duplicate slots, past-day filtering, transfer origin/time, approved inventory/search/empty ratings, five no-shows in thirty calendar days, credential checks and duplicate appeals, rejection and manual unlock reasons, notification ownership and blocked-account public navigation.
