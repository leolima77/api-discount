# Discount API – gRPC on .NET

A **gRPC** service in **.NET** that creates and consumes discount codes.  
The project focuses on **performance**, **observability**, **testing** and **containerization**.

---

## Goals

- Expose a gRPC API with two operations:
  - **Generate**: create `N` unique codes with length 7–8.
  - **UseCode**: consume a code **once** (idempotent at the DB level).
- Keep the service **fast**, **simple**, and **reliable**.
- Provide good **logging**, **metrics/traces**, **health checks**, and **graceful shutdown**.
- Ship as a **Docker** container, with a **CI pipeline** for the image.

---

## Tech Stack

- **.NET 8**, **ASP.NET Core gRPC**
- **PostgreSQL** (relational DB)
- **Dapper** (lightweight data access)
- **Serilog** (structured logging)
- **OpenTelemetry** (metrics + traces; OTLP exporter)
- **ASP.NET Core HealthChecks**
- **Docker** (containerization)
- **Azure Pipelines** (CI for container builds)
- **xUnit + Moq + FluentAssertions** (tests)

---

## Project Structure

```
src/
  ApiDiscount/                 # gRPC host (single-project layout)
    Contracts/                 # DTOs (requests/responses for service layer)
    Database/
      Interfaces/              # repository interfaces
      Repositories/            # repository implementations (Dapper)
      DapperContext.cs
    Domains/                   # domain entities (e.g., DiscountCode)
    Helpers/                   # helpers (e.g., CodeGenerator)
    Protos/                    # discount.proto (gRPC contracts)
    Services/
      Grpc/                    # gRPC service(s) implementation
      Interfaces/              # service interfaces
      Repositories/            # service implementations
    Program.cs / Startup.cs    # DI, Serilog, OTel, HealthChecks, Kestrel, GC
    appsettings.json
tests/
  ApiDiscount.Tests/           # unit tests
```

---

## gRPC API

**Proto:** `Protos/discount.proto`

- `rpc Generate(GenerateRequest) returns (GenerateResponse);`  
  - Input: `count` (1..2000), `length` (7 or 8)  
  - Output: `result: bool` (true when all requested codes were created)

- `rpc UseCode(UseCodeRequest) returns (UseCodeResponse);`  
  - Input: `code: string` (7–8 chars)  
  - Output: `result: UseResult` (`USE_OK`, `USE_INVALID`, `USE_ALREADYUSED`)


---

## Database (PostgreSQL)

Run file discount.sql at project root folder

**Why this design**

- **Primary key on `code`** ensures uniqueness during generation.  
- **Check constraint** enforces length rules.  
- **Partial index** speeds up reads when checking “unused” codes.

---

## Performance & Concurrency

- **Dapper** for minimal overhead on database calls.
- **Batch insert** available via repository method `CreateManyAsync(codes)`:
  ```sql
  insert into discount.discount_code (code)
  select c from unnest(@codes) as t(c)
  on conflict (code) do nothing;
  ```
- Option for **bounded parallelism** when generating many codes.
- **Connection pooling** enabled in Npgsql (see connection string).

---

## Observability

- **Serilog** for structured logs (console sink; enrichers for machine/process/thread).
- **OpenTelemetry** for **traces** and **metrics**; export via **OTLP** (configurable endpoint).
- **Health Checks** mapped to `/healthz` for readiness/liveness.
- **Graceful Shutdown**: the host waits for in-flight requests to complete.

---

## Testing

- **xUnit** as test framework.  
- **Moq** for repository/service mocks.  
- **FluentAssertions** for readable assertions.

Coverage includes:

- Code normalization (trim/uppercase) and length validation (7–8).
- Service → repository delegation.
- “Use once” behavior.
- Batch insert count.
- Error cases (invalid code length).

Run tests:

```bash
dotnet test
```

---

## Configuration

`appsettings.{env}.json`

Runtime tuning (example):

- `DOTNET_ThreadPool_MinThreads=200`
- Server GC (enabled in project config)
- HTTP/2 limits for high concurrency

---

## Run Locally

```bash
# build
dotnet build

# apply the SQL above to your Postgres (psql or a migration tool)

# run
dotnet run --project src/ApiDiscount
```

Test with **grpcurl** (plaintext in dev):

```bash
# Generate 5 codes of length 8
grpcurl -plaintext -d '{"count":5,"length":8}' \
  localhost:5000 discount.DiscountService/Generate

# Use one of the returned codes
grpcurl -plaintext -d '{"code":"ABC12345"}' \
  localhost:5000 discount.DiscountService/UseCode
```

> If you enable gRPC reflection in Development, tools like grpcurl/Postman can discover services without the proto.

---

## Docker

**Dockerfile** (multi-stage) is included. Typical commands:

```bash
# build image
docker build -t yourrepo/discount-api:latest .

# run (needs Postgres reachable at the connection string)
docker run -p 5000:5000 \
  -e ConnectionStrings__Postgres="Host=host.docker.internal;Port=5432;Username=postgres;Password=postgres;Database=discountdb;Pooling=true;Maximum Pool Size=200;" \
  yourrepo/discount-api:latest
```

---

## Azure Pipelines (CI Example)

A simple pipeline can:

1) Restore, build, and test  
2) Build the Docker image  
3) Push to a container registry

---
