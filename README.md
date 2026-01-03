 NorthwindTraders — Enterprise Sample API

A production-style ASP.NET Core 8 Web API demonstrating \*\*Clean Architecture\*\*, modern backend practices, and enterprise-ready design.

This project models a real business system for managing products, customers, orders, suppliers, and inventory using the classic Northwind domain.


## Architecture Overview
This solution follows \*\*Clean Architecture\*\* with strict separation of concerns:

## Error Handling & Observability

- All unhandled errors return RFC 7807 `application/problem+json` (ProblemDetails).
- Validation failures return `ValidationProblemDetails` with a consistent `errors` payload.
- Each request has an `X-Correlation-Id` (accepted or generated) and responses echo it back.
- `traceId` and `correlationId` are included in ProblemDetails responses and Serilog request logs.

## API Versioning

- Routes use URL segment versioning: `/api/v{version}/...` (e.g. `/api/v1/customers`).

## Integration Tests

- Tests run the API host with an InMemory EF Core database.
- `INorthwindDbContext` is wired to `NorthwindTradersContext` in both production and test hosts.








