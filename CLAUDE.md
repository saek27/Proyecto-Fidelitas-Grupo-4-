# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sistema de Gestión - Óptica Comunal: An administrative and clinical management system for a network of optical stores, built with **.NET 8** and **Clean Architecture**.

## Architecture

```
OC.Core/      - Domain layer: Entities, repository interfaces, services
OC.Data/      - Data layer: EF Core DbContext, repositories, configurations, migrations
OC.Web/       - Presentation layer: ASP.NET Core MVC controllers, views, viewmodels
```

**Key architectural patterns:**
- Clean Architecture with 3-layer separation
- Entity Framework Core Code First with migrations (migrations assembly: OC.Data)
- Generic Repository pattern (`IGenericRepository<T>`)
- Cookie-based authentication with 8-hour session timeout
- Background services for SLA monitoring, ticket auto-close, and appointment reminders

**Database initialization:** `DbInitializer` runs at startup via `Ensure*` methods that create tables/columns directly with raw SQL when migrations fail or aren't applied yet. This is intentional—it ensures schema readiness without relying solely on EF migrations.

## Build & Run Commands

```bash
# Build entire solution
dotnet build

# Run the web project (http://localhost:5000 by default)
dotnet run --project OC.Web

# Run with specific URL
dotnet run --project OC.Web --urls "http://localhost:5000"
```

## Database Migrations

```powershell
# Via Package Manager Console in Visual Studio
Update-Database -StartupProject OC.Web

# Via .NET CLI (from solution root)
dotnet ef database update --project OC.Data --startup-project OC.Web
```

The startup project for EF commands is **OC.Web**, but the migrations live in **OC.Data**.

## Project Dependencies

- **OC.Web** references OC.Core and OC.Data
- **OC.Data** references OC.Core
- **OC.Core** has no dependencies on other projects (pure domain)

## Key Implementation Notes

- **Decimal handling**: App forces `en-US` invariant culture for all threads to avoid decimal parsing issues (period vs comma)
- **Expediente-Cita relationship**: One-to-one via `CitaId` foreign key with `DeleteBehavior.Restrict`
- **File upload limits**: 10 MB max for multipart forms (configured in `Program.cs`)
- **TOTP 2FA**: Patients use TOTP-based two-factor authentication during registration (see `ITotpService`)
- **Background services**: `SLAMonitorService`, `TicketAutoCloseService`, `RecordatorioCitasBackgroundService` are registered as hosted services
- **Price integrity in Ventas**: Prices come exclusively from the database (`Producto.Precio`, `TecnologiaLente.Precio`, `Aro.Precio`). Users cannot manually set prices in sales forms — prices are read-only via `data-precio` attributes on select options. Total = `(subtotal - descuento%) * 1.13` (IVA included)
- **Inventory consolidation**: All inventory (Productos, TecnologiaLente, Aro) managed via `InventoryController` with section tabs on `Views/Inventory/Index`. Create/Edit redirects always return to `Inventory/Index?seccion=X`
- **Aro soft-delete**: `Aro.Activo = false` on delete, not a hard delete

## Connection String

- Production: Azure SQL Server (`server-opticacomunal.database.windows.net`)
- Development: Local SQL Server (see `appsettings.Development.json` override)
- Connection is configured in `OC.Web/appsettings.json` under `ConnectionStrings:DefaultConnection`