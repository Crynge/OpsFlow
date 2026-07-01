[![CI](https://github.com/Crynge/OpsFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/Crynge/OpsFlow/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

# OpsFlow

**Enterprise marketing operations & budget intelligence platform.**

SOX-compliant budget management, multi-level approval workflows, simplex-based budget optimization, and financial forecasting for marketing operations.

---

## Solution Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        OpsFlow CLI                              │
│  budget create | budget approve | budget report | optimize      │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                       OpsFlow API                               │
│  REST endpoints for budget CRUD, approvals, forecasting         │
└───────────┬──────────────────────────────────┬──────────────────┘
            │                                  │
┌───────────▼──────────────┐    ┌──────────────▼──────────────────┐
│     OpsFlow.Core         │    │      OpsFlow.Tests              │
│                           │    │                                  │
│  ┌─────────────────────┐  │    │  ┌──────────────────────────┐   │
│  │ Budget Management   │  │    │  │ BudgetPlanService.Test   │   │
│  │ - Plans, line items │  │    │  │ ApprovalWorkflow.Test    │   │
│  │ - Fiscal periods    │  │    │  │ SimplexSolver.Test       │   │
│  ├─────────────────────┤  │    │  │ BudgetForecaster.Test    │   │
│  │ Approval Workflow   │  │    │  │ ComplianceAuditor.Test   │   │
│  │ - Multi-level chain │  │    │  └──────────────────────────┘   │
│  │ - Escalation rules  │  │    └──────────────────────────────────┘
│  ├─────────────────────┤  │
│  │ Simplex Optimizer   │  │
│  │ - Linear programming│  │
│  │ - Constraint solver │  │
│  ├─────────────────────┤  │
│  │ Budget Forecasting  │  │
│  │ - Time-series ARIMA │  │
│  │ - Seasonality       │  │
│  ├─────────────────────┤  │
│  │ SOX Compliance      │  │
│  │ - Audit trails      │  │
│  │ - Segregation checks│  │
│  └─────────────────────┘  │
└────────────────────────────┘
```

## Domain Model

```
┌────────────────────────────────────────────────────────────────┐
│                        BUDGET PLAN                              │
├────────────────────────────────────────────────────────────────┤
│  ID:          bp-2026-Q3                                       │
│  Name:        Q3 Enterprise Marketing                          │
│  Fiscal Year: 2026                                             │
│  Status:      PENDING_APPROVAL  (created 2026-06-15)          │
├────────────────────────────────────────────────────────────────┤
│  LINE ITEMS                              AMOUNT      APPROVED  │
│  ────────────────────────────────────────────────────────────  │
│  Digital Display                         $120,000    ✅       │
│  Search Ads                              $85,000     ✅       │
│  Social Media                            $65,000     ⏳       │
│  Content Production                      $45,000     ⏳       │
│  Agency Fees                             $30,000     ❌       │
├────────────────────────────────────────────────────────────────┤
│  TOTAL:                                  $345,000              │
│  REMAINING:                              $155,000              │
├────────────────────────────────────────────────────────────────┤
│  APPROVAL CHAIN:  Manager → Director → Finance → CFO           │
│  Current Step:    Director Pending                             │
└────────────────────────────────────────────────────────────────┘
```

## CLI Quick Start

```bash
# Create a budget plan
dotnet run -- budget create "Q3 Enterprise" 2026 500000

# Approve a line item
dotnet run -- budget approve bp-2026-Q3 --line "Digital Display" --role manager

# Run budget optimization
dotnet run -- optimize simplex bp-2026-Q3 --goal maximize_reach

# Generate forecast
dotnet run -- report forecast bp-2026-Q3 --months 6
```

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/budgets` | Create budget plan |
| `GET` | `/api/budgets` | List budget plans |
| `GET` | `/api/budgets/{id}` | Get budget details |
| `POST` | `/api/budgets/{id}/approve` | Approve a line item |
| `POST` | `/api/budgets/{id}/optimize` | Run simplex optimization |
| `GET` | `/api/budgets/{id}/forecast` | Get budget forecast |

## Simplex Optimization

```csharp
var solver = new SimplexSolver();
solver.AddVariable("digital_display", 0, 200000);
solver.AddVariable("search_ads", 0, 150000);
solver.AddConstraint("total", "digital_display + search_ads <= 300000");
solver.SetObjective("maximize", "0.05 * digital_display + 0.08 * search_ads");

var result = solver.Solve();
// digital_display: $180,000  search_ads: $120,000  objective: 18,600
```

## SOX Compliance

```csharp
var auditor = new ComplianceAuditor();
var issues = auditor.Audit(budgetPlan);

foreach (var issue in issues)
{
    Console.WriteLine($"[{issue.Severity}] {issue.Description}");
}

// Sample output:
// [Error] Segregation of Duties: same user created and approved line item
// [Warning] Budget threshold exceeded: 95% of total allocated
```

## Projects

```
OpsFlow.sln
├── src/
│   ├── OpsFlow.Core/        # Domain models, services, stores
│   ├── OpsFlow.Api/         # REST API (ASP.NET Core)
│   └── OpsFlow.Cli/         # Command-line interface
└── tests/
    └── OpsFlow.Tests/       # xUnit test suite (5 test classes)
```
