# OpsFlow

Enterprise marketing operations & budget intelligence platform.

## Architecture

- **OpsFlow.Core** - Domain logic, models, services, optimization engine
- **OpsFlow.Api** - ASP.NET minimal API for budget management
- **OpsFlow.Cli** - Command-line interface for admin tasks
- **OpsFlow.Tests** - xUnit test suite

## Features

- Budget planning and tracking (Draft/Active/Frozen/Closed)
- Multi-level approval workflows with escalation
- Campaign budget optimization using Simplex algorithm (Maximize ROAS, Minimize CPA, Maximize Reach)
- Compliance auditing with SOX reporting and segregation of duties
- Budget variance alerts (>10% threshold)
- Rolling forecast and burn rate analysis
- Vendor spend analysis
- CSV/JSON report export

## Getting Started

```
dotnet restore
dotnet build
dotnet test
dotnet run --project src/OpsFlow.Cli -- serve
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/health | Health check |
| GET | /api/budgets | List all budget plans |
| POST | /api/budgets | Create budget plan |
| GET | /api/budgets/{id} | Get budget plan |
| PUT | /api/budgets/{id}/reallocate | Reallocate budget |
| POST | /api/budgets/{id}/freeze | Freeze budget |
| GET | /api/budgets/{id}/forecast | Get budget forecast |
| POST | /api/optimize | Run allocation optimization |
| POST | /api/workflows/approve | Approve workflow request |
| GET | /api/compliance/audit-log | Get audit trail |
| GET | /api/reports/budget-vs-actual | Budget vs actual report |

## CLI Usage

```
dotnet run --project src/OpsFlow.Cli -- budget create "FY2026" 2026 500000
dotnet run --project src/OpsFlow.Cli -- budget list
dotnet run --project src/OpsFlow.Cli -- optimize run
dotnet run --project src/OpsFlow.Cli -- report generate budget-vs-actual
dotnet run --project src/OpsFlow.Cli -- serve
```
