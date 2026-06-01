# Óptica Comunal - Architecture Diagram

## System Overview

```mermaid
graph TB
    subgraph "Presentation Layer - OC.Web"
        Controllers["Controllers\nVentasController, PacientesController\nInventoryController, ReportesController\nTecnologiasController, ArosController\nTicketsController, PedidosController"]
        Views["Views / Razor Pages\nInventory, Reportes, Ventas\nPacientes, Tickets, Landing"]
        ViewModels["ViewModels\nVentaCreateViewModel, ReporteVentasViewModel\nReporteFidelizacionViewModel, ReporteDemandaViewModel"]
        Middleware["Middleware\nAuth, Error Handling"]
    end

    subgraph "Domain Layer - OC.Core"
        Entities["Entities\nProducto, TecnologiaLente, Aro\nPaciente, Cita, Expediente\nVenta, Ticket, Pedido, Sucursal"]
        Contracts["Repository Interfaces\nIGenericRepository<T>"]
        Services["Domain Services\nITotpService"]
    end

    subgraph "Data Layer - OC.Data"
        DbContext["AppDbContext\nEF Core ORM"]
        Repositories["Repositories\nIGenericRepository Impl"]
        Configurations["Entity Configurations\nFluent API"]
        Migrations["Migrations\nOC.Data Assembly"]
        DbInitializer["DbInitializer\nRaw SQL Schema Setup"]
    end

    subgraph "Infrastructure"
        DB["Azure SQL Server\n(LOCAL SQL Dev)"]
        FileStorage["File Storage\nwwwroot/uploads"]
        BackgroundJobs["Background Services\nSLAMonitorService\nTicketAutoCloseService\nRecordatorioCitasBackgroundService"]
    end

    Controllers --> ViewModels
    Controllers --> Entities
    Views --> ViewModels
    Views --> Entities
    ViewModels --> Entities
    Repositories --> Entities
    DbContext --> Entities
    DbContext --> Configurations
    Repositories --> Contracts
    Middleware --> Controllers
    BackgroundJobs --> Repositories
    Repositories --> DB
    FileStorage --> Controllers
```

## Layer Dependencies

```mermaid
graph LR
    A["OC.Web\nPresentation"] --> B["OC.Core\nDomain"]
    A --> C["OC.Data\nData"]
    C --> B
    DB["Database\nAzure SQL"] -.-> C
```

## Key Entity Relationships

```mermaid
erDiagram
    Producto {
        int Id PK
        string Nombre
        string SKU
        decimal CostoUnitario
        int Stock
        bool Activo
        string RutaImagen
    }
    TecnologiaLente {
        int Id PK
        string Nombre
        decimal Precio
    }
    Aro {
        int Id PK
        string Nombre
        string SKU UK
        decimal Precio
        int Stock
        bool Activo
        string RutaImagen
    }
    Venta {
        int Id PK
        int ProductoId FK
        decimal Subtotal
        decimal Descuento
        decimal Total
        string MetodoPago
        string ReferenciaPago
        string RutaComprobante
        int? TecnologiaLenteId FK
        int? AroId FK
    }
    Paciente {
        int Id PK
        string Nombre
        string Cedula
        string Email
        string Telefono
    }
    Cita {
        int Id PK
        int PacienteId FK
        int? ExpedienteId FK
        string Estado
        DateTime FechaHora
    }
    Expediente {
        int Id PK
        int CitaId UK
        string Observaciones
    }
    Sucursal {
        int Id PK
        string Nombre
        string Ubicacion
    }
    Ticket {
        int Id PK
        int? PacienteId FK
        int? SucursalId FK
        string Asunto
        string Estado
    }
    Pedido {
        int Id PK
        int? ProveedorId FK
        DateTime Fecha
        string Estado
    }
    Producto ||--o| Venta : " FK"
    TecnologiaLente ||--o| Venta : " FK"
    Aro ||--o| Venta : " FK"
    Paciente ||--o| Cita : " FK"
    Cita ||--o| Expediente : " 1:1 via CitaId"
```

## Request Flow - Inventory Feature

```mermaid
sequenceDiagram
    participant User
    participant InventoryController
    participant IGenericRepository
    participant AppDbContext
    participant Database

    User->>InventoryController: GET /Inventory?seccion=productos
    InventoryController->>IGenericRepository: GetPagedAsync(page, pageSize, filter)
    IGenericRepository->>AppDbContext: EF Core Query
    AppDbContext->>Database: SQL Query
    Database-->>AppDbContext: Producto rows
    AppDbContext-->>IGenericRepository: List<Producto>
    IGenericRepository-->>InventoryController: PagedResult<Producto>
    InventoryController->>InventoryController: ViewBag.PaginationInfo
    InventoryController-->>User: View with 3 tab sections

    User->>InventoryController: POST Filter producto
    InventoryController->>InventoryController: filtroProducto parameter
    InventoryController->>IGenericRepository: GetPagedAsync(filter: Contains)
    loop Filter logic in controller
        IGenericRepository->>AppDbContext: filtered query
    end
    Database-->>AppDbContext: filtered rows
    AppDbContext-->>IGenericRepository: filtered list
    IGenericRepository-->>InventoryController: PagedResult + filters preserved
    InventoryController-->>User: View with filtered results
```

## Project Structure

```
OC.Solution/
├── OC.Core/                    # Domain Layer (no dependencies)
│   ├── Domain/
│   │   └── Entities/           # Producto, TecnologiaLente, Aro, Venta, etc.
│   ├── Contracts/
│   │   └── IRepositories/     # IGenericRepository<T>
│   └── Services/               # ITotpService
│
├── OC.Data/                    # Data Layer (depends on OC.Core)
│   ├── Context/
│   │   ├── AppDbContext.cs     # EF Core DbContext
│   │   └── DbInitializer.cs     # Raw SQL schema setup
│   ├── Repositories/           # IGenericRepository implementations
│   ├── Configurations/          # Fluent API entity configs
│   └── Migrations/             # EF Migrations assembly
│
├── OC.Web/                     # Presentation Layer (depends on Core + Data)
│   ├── Controllers/            # MVC Controllers
│   ├── Views/
│   │   ├── Inventory/          # Consolidated inventory tabs
│   │   ├── Reportes/           # Ventas, Fidelizacion, Demanda
│   │   ├── Ventas/
│   │   ├── Pacientes/
│   │   └── Landing/
│   ├── ViewModels/
│   ├── wwwroot/
│   │   ├── css/
│   │   ├── uploads/            # Producto, Aro, Comprobante images
│   │   └── js/
│   ├── Program.cs              # DI, Middleware, Background Services
│   └── appsettings.json
│
└── CLAUDE.md
```

## Database Schema (Key Tables)

```mermaid
erDiagram
    Producto {
        int Id PK
        string Nombre
        string SKU UK
        decimal CostoUnitario
        int Stock
        bool Activo
        string RutaImagen
    }
    TecnologiaLente {
        int Id PK
        string Nombre
        decimal Precio
    }
    Aro {
        int Id PK
        string Nombre
        string SKU UK
        decimal Precio
        int Stock
        bool Activo
        string RutaImagen
    }
```