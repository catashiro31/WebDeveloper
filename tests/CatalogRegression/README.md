# Catalog regression check

Run against a migrated **development/test PostgreSQL database**:

```bash
CATALOG_TEST_CONNECTION='Host=127.0.0.1;Database=Docbooking;Username=postgres' \
  dotnet run --project tests/CatalogRegression/CatalogRegression.csproj
```

The check reads both catalog lists before creating records, then uses the real
admin and public services to verify that creates, updates, verification and soft
deletes are immediately reflected in subsequent reads. It reuses one memory
cache across requests, matching the scenario that previously returned old lists
for 12 hours.

Uploads use a test implementation of `IFileUploadService`; this check does not
send files to Cloudinary or verify external storage. Database queries and all
catalog mutations run against PostgreSQL. Each run generates unique names and
removes only its own records in `finally`.

It also registers a doctor with the selected specialty and facility, verifies
that both IDs and names can be read back, checks that pending profiles remain
private, and checks that an approved profile appears with matching filters and
the correct specialty/facility names in its public card.
