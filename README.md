# VivaBev SCM

[![CI](https://github.com/VictoryOmowumi/vivabev-scm/actions/workflows/ci.yml/badge.svg)](https://github.com/VictoryOmowumi/vivabev-scm/actions/workflows/ci.yml)

Enterprise supply chain management platform for VivaBev Beverages Ltd., a fictional beverage manufacturer.
A portfolio project built as a modular monolith: ASP.NET Core 10, EF Core, SQL Server, and a React frontend (coming).

## Modules

| Module | Status |
| --- | --- |
| Catalogue: items, categories, units of measure | Done |
| Suppliers: onboarding, approval workflow, category approvals | Done |
| Identity and Organisation | Next |
| Procurement, Inventory (receipts), Finance (budgets, three-way match) | Planned for release 1 |

## Run it locally

Prerequisites: Docker Desktop, .NET 10 SDK.

    docker compose -f infra/local/docker-compose.yml up -d
    dotnet tool restore
    dotnet run --project src/api/VivaBev.Api.Host

In a second terminal (needs Node 26 LTS):

    cd src/web
    npm install
    npm run dev

Then open http://localhost:5174. Start the API with `--launch-profile https`.

See `docs/` for the product definition, architecture and data model.