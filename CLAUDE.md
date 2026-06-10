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

## UI Design System (vistas CRUD)

**Regla importante:** TODAS las vistas de tipo Index/Create/Edit/Details deben usar el patrón estándar definido en `wwwroot/css/site.css` para mantener consistencia visual. Clases reutilizables:

### Estructura de un Index
```html
<div class="container-fluid px-4">
    <div class="page-header animate-fade-up">
        <div class="page-header-text">
            <div class="page-header-icon"><i class="bi bi-xxx"></i></div>
            <div>
                <h1 class="page-title">Título</h1>
                <p class="page-subtitle"><i class="bi bi-info-circle me-1"></i> Descripción / conteo.</p>
            </div>
        </div>
        <div class="page-actions">
            <a class="btn btn-brand"><i class="bi bi-plus-circle me-1"></i> Nuevo</a>
        </div>
    </div>

    @* alertas con animate-fade-up *@

    <div class="card-dashboard animate-fade-up delay-1 mb-3"> @* search/filter bar *@ </div>

    <div class="card-dashboard animate-fade-up delay-2">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light"> ... </thead>
                    <tbody> ... </tbody>
                </table>
            </div>
            @if (!items.Any()) { <div class="empty-state"> ... </div> }
        </div>
    </div>

    @await Html.PartialAsync("_Pagination", pagination)
</div>
```

### Clases clave del design system
- **Botones primarios**: `<a class="btn btn-brand">` (gradiente azul, sombra)
- **Acciones en filas (icon-only)**: `.action-buttons` contenedor + `.btn-action.btn-action-{info|primary|success|danger|secondary}` (32×32 cuadrado, hover cambia color)
- **Avatar en celda**: `.entity-cell` + `.entity-avatar.bg-{info|...}-subtle.text-{info|...}-emphasis`
- **Badge de estado**: `.status-badge` + `.status-badge-active` (verde) | `.status-badge-inactive` (gris). Para otros colores, inline `style="background:rgba(...);color:...;"`
- **Métrica/contador (ej. # pedidos)**: `.metric-tile` (gris neutro) | `.metric-tile-active` (azul, cuando hay datos)
- **Contacto en celda**: `.contact-line` (línea con icono + texto)
- **Empty state**: `.empty-state` con `.empty-state-icon` + `.empty-state-title` + `.empty-state-text`
- **Form header**: `.form-header` + `.form-header-icon` + `.form-header-text` (título + descripción)
- **Details header**: `.details-header` + `.details-header-info` + `.details-icon` + `.details-title` + `.details-actions`
- **Info card (Details)**: `.info-card` + `.info-card-label` + `.info-card-value`
- **Sección dentro de form**: `.section-header`
- **Filter bar**: `.filter-bar` (card-body con padding reducido)
- **Paginación**: SIEMPRE usar `@await Html.PartialAsync("_Pagination", pagination)` con un `PaginationInfo` configurado. NO escribir `<nav><ul class="pagination">` inline.
- **Animaciones de entrada**: `.animate-fade-up` (entero), `.delay-1`, `.delay-2`, `.delay-3`
- **Moneda**: SIEMPRE `@$"₡{valor:N0}"` (N0 con la app en en-US da separador de coma)

### Paleta de colores (de site.css)
- `--color-primary: #0f172a` (slate-900)
- `--color-brand: #3b82f6` (blue-500) — primario
- `--color-brand-dark: #2563eb` (blue-600)
- `--color-accent: #06b6d4` (cyan-500)
- `--color-success: #10b981` (emerald-500)
- `--color-warning: #f59e0b` (amber-500)
- `--color-danger: #ef4444` (red-500)
- `--color-text: #1e293b`
- `--color-text-muted: #64748b`
- `--color-border: #e2e8f0`
- `--color-bg: #f1f5f9`
- `--color-surface: #ffffff`

### Vistas excluidas del patrón estándar
- **Home/Index**: usa dark glassmorphism propio (con canvas 3D). Mantiene paleta del design system.
- **Inventory/Index**: tiene tabs/secciones, sigue usando `.card-dashboard` y `.table-light` pero con su propio header.
- **Tickets/Index**: usa badges SLA custom (deja el sistema SLA intacto).
- **Reportes/Index**: tiene CSS propio con sus colores de marca.
- **PacienteDashboard/Index, Landing/*, Account/*, PacienteAccount/*, Historial/*, SolicitudesCitas/* (parcial), CitasPublicas/* (vistas de paciente, no admin)**: mantienen su estilo contextual.

### Cuándo NO usar IDENTITY_INSERT
La convención del proyecto es usar `DbInitializer.Ensure*` con SQL crudo en runtime para cambios de schema menores (columnas nuevas, conversiones de tipo) y NO generar migraciones para esos casos. El `AppDbContextModelSnapshot` queda intencionalmente desincronizado; los `Ensure*` compensan al arranque.

**Cuándo SÍ generar migración:** cambios estructurales grandes (nuevas tablas, FKs nuevas, renombrar columnas).

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

## Database Initialization (DbInitializer)

`DbInitializer` runs at startup and uses raw SQL `CREATE TABLE`/`ALTER TABLE` statements (via `Ensure*` methods) to guarantee schema readiness independently of EF migrations. This pattern handles cases where migrations haven't been applied or have inconsistent history. Key tables ensured:

- `OrdenesTrabajo`
- `EnviosNotificacion`
- `Citas` (notification columns)
- `Paciente` (lockout columns)
- `Usuario` (security columns)
- `Permiso` (ruta_documento_incapacidad column)
- `Producto` (RutaImagen column)

## Visual Architecture

See [architecture-diagram.md](architecture-diagram.md) for Mermaid diagrams showing:
- System overview and layer dependencies
- Entity relationships and key database schema
- Request flow for the inventory feature

## Connection String

- Production: Azure SQL Server (`server-opticacomunal.database.windows.net`)
- Development: Local SQL Server (see `appsettings.Development.json` override)
- Connection is configured in `OC.Web/appsettings.json` under `ConnectionStrings:DefaultConnection`