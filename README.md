# StrivoForklift

Azure Function App that reads forklift events from an Azure Storage Queue and persists them in a database. When a newer event (with a more recent timestamp) arrives for an existing forklift ID, the stored record is updated; older or duplicate messages are ignored.

## Architecture

```
Azure Storage Queue ("forklift-events")
        │
        ▼
ForkliftQueueFunction  (Azure Functions v4 – .NET 8 isolated worker)
        │
        ▼
ForkliftDbContext  (Entity Framework Core)
        │
        ▼
SQLite (local dev) / Azure SQL (production)
```

## Queue Message Format

Messages placed on the `forklift-events` queue must be JSON-encoded:

```json
{
  "id": "forklift-1",
  "timestamp": "2024-06-01T08:30:00Z",
  "status": "active",
  "location": "warehouse-A"
}
```

| Field       | Type             | Description                                    |
|-------------|------------------|------------------------------------------------|
| `id`        | string (required)| Unique identifier for the forklift/entity      |
| `timestamp` | ISO-8601 string  | Event time – used to determine message recency |
| `status`    | string (optional)| Operational status (e.g. `active`, `idle`)     |
| `location`  | string (optional)| Current location of the forklift               |

## Upsert Logic

| Scenario                            | Action                        |
|-------------------------------------|-------------------------------|
| No existing record for `id`         | **Insert** new record         |
| Incoming timestamp **>** stored     | **Update** the existing record|
| Incoming timestamp **≤** stored     | **Skip** (no change)          |

## Project Structure

```
StrivoForklift.sln
src/
  StrivoForklift/
    ForkliftQueueFunction.cs   # Queue-triggered Azure Function
    Program.cs                 # Host / DI configuration
    host.json
    local.settings.json        # Local dev settings (not published)
    Models/
      QueueMessage.cs          # Deserialized queue payload
      ForkliftEvent.cs         # Database entity
    Data/
      ForkliftDbContext.cs     # EF Core DB context
tests/
  StrivoForklift.Tests/
    ForkliftQueueFunctionTests.cs  # xUnit tests (in-memory DB)
```

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (Storage emulator)

### Run locally

```bash
# Start Azurite (Storage emulator)
azurite --silent &

# Start the Function App
cd src/StrivoForklift
func start
```

### Run tests

```bash
dotnet test
```

## Configuration

| Setting                  | Description                                              |
|--------------------------|----------------------------------------------------------|
| `StorageConnectionString`| Azure Storage connection string for the queue trigger    |
| `ConnectionStrings:SqlConnection` | Database connection string (defaults to SQLite `forklift.db`) |

For production deployment, set `StorageConnectionString` to your Azure Storage Account connection string and `SqlConnection` to your Azure SQL connection string in the Function App's Application Settings.
