Run against a new isolated PostgreSQL database with .NET 10:

```bash
CREDENTIAL_TEST_CONNECTION='Host=localhost;Database=credential_test;Username=postgres' dotnet run --project tests/CredentialRegression
```

Applies migrations and writes unique fixtures. Uploads are fake. Covers legacy missing diplomas, required evidence, unchanged approved profile while pending/rejected, duplicate pending requests, mandatory rejection reasons, notifications, degree/diploma/certificate approval and repeat decisions. Does not exercise browser rendering or HTTP authentication.
