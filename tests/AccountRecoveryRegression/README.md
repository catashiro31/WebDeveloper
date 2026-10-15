Run only against a new isolated PostgreSQL test database. The runner creates the schema using the current EF model and leaves unique test fixtures. It does not send email.

```sh
ACCOUNT_RECOVERY_TEST_CONNECTION='Host=127.0.0.1;Database=account_recovery_test;Username=postgres' dotnet run --project tests/AccountRecoveryRegression
```

Checks patient and doctor accounts blocked with leftover password-reset codes: appeal submission, direct unlock, appeal approval, sign-in after unlock, clearing one-time codes and blocking email-verification links from undoing a ban. Also verifies genuinely unverified registrations must still verify their email.
