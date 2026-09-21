# Approval Tasks API

Small ASP.NET Core Web API for managing approval tasks.

## Requirements

* .NET 10 SDK or later

## Run API

```bash
dotnet restore
dotnet run
```

OpenAPI specification:

```text
http://localhost/openapi/v1.json
```

> The actual port may differ depending on `launchSettings.json` or ASP.NET Core configuration.

## Run tests

```bash
dotnet test
```

## API examples

### Create task

```http
POST /api/approval-tasks
Content-Type: application/json
```

```json
{
  "documentNumber": "DOC-001",
  "name": "Test document",
  "assigneeId": "11111111-1111-1111-1111-111111111111"
}
```

The task is created with status `Assigned` and an empty history.

### Start task

```http
POST /api/approval-tasks/{id}/actions
Content-Type: application/json
```

```json
{
  "actorId": "11111111-1111-1111-1111-111111111111",
  "action": "Start"
}
```

Status changes:

```text
Assigned -> InProgress
```

### Approve task

```json
{
  "actorId": "11111111-1111-1111-1111-111111111111",
  "action": "Approve"
}
```

Status changes:

```text
InProgress -> Approved
```

### Reject task

```json
{
  "actorId": "11111111-1111-1111-1111-111111111111",
  "action": "Reject",
  "comment": "Document contains incorrect information"
}
```

Status changes:

```text
InProgress -> Rejected
```

A comment is required when rejecting a task.

### Get task

```http
GET /api/approval-tasks/{id}
```

Returns the current task state and successful action history.

## Concurrency

The service uses a process-local `lock` to make the complete state transition atomic.

The status validation, status update, timestamp update and history insertion are performed inside the same critical section. Therefore, when two final actions are executed concurrently, only one can successfully change an `InProgress` task to a final state. The other request receives a conflict error.

## Known limitations

* Data is stored only in memory and is lost when the application stops.
* The concurrency protection works only within a single application process.
* There is no authentication or authorization system; `actorId` is provided by the client to simulate the current user.
* There is no persistent database.
