# Plan: Pestaña Landing en Inventario + DescripcionCorta en Aro

**Fecha**: 2026-08-16
**Contexto**: Session anterior dejó un análisis del LandingController.Index() — problemas:
sin `orderBy`, sin control fino sobre qué items van al carrusel vs destacados, hard-coded
máximos (6 carrusel / 8 destacados). Además Aro no tiene `DescripcionCorta`, pero la vista
pública la usa (`Descripcion = a.SKU` actualmente).

**Decisiones tomadas en sesión**:
1. Pestaña 4 en `Inventory/Index` → "Landing"
2. Dos zonas drag-and-drop: Carrusel (máx 6) + Destacados (máx 8), con slots numerados
3. Zona "Disponibles (X)" abajo con items marcados fuera de slots → drag hacia arriba los promueve
4. **Cascada automática**: arrastrar a slot ⇔ marcar `Destacado`/`MostrarEnLanding`; sacar ⇔ desmarcar
5. Modelo: dos tablas nuevas (`CarruselItems`, `DestacadosItems`), una fila por slot
6. **Aro gana `DescripcionCorta`** (nvarchar(500), opcional)

**Resultado esperado**: admin maneja TODO el landing desde una sola pantalla, sin tocar
las pestañas Productos/Aros. Flags quedan derivados del slot.

---

## Cambios en BD (OC.Data/Context/DbInitializer.cs)

Tres nuevas rutinas Ensure*, llamadas desde `Program.cs` junto a las existentes:

1. **`EnsureAroDescripcionCortaColumn`**
   - `ALTER TABLE Aros ADD DescripcionCorta nvarchar(500) NULL`
   - Wrap en `IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Aros') AND name='DescripcionCorta')`
   - Idempotente.

2. **`EnsureCarruselItemsTable`**
   - Tabla `CarruselItems` con:
     - `Id INT IDENTITY(1,1) PK`
     - `Posicion TINYINT NOT NULL` (1..6, UNIQUE)
     - `Tipo NVARCHAR(20) NOT NULL` ('Producto'|'Aro')
     - `ProductoId INT NULL` (FK → Productos.Id, ON DELETE CASCADE)
     - `AroId INT NULL` (FK → Aros.Id, ON DELETE CASCADE)
     - `CHECK (ProductoId IS NOT NULL OR AroId IS NOT NULL)`
     - `CHECK ((Tipo='Producto' AND ProductoId IS NOT NULL AND AroId IS NULL) OR (Tipo='Aro' AND AroId IS NOT NULL AND ProductoId IS NULL))`
   - Índices: UNIQUE(`Posicion`), `IX_CarruselItems_ProductoId`, `IX_CarruselItems_AroId`

3. **`EnsureDestacadosItemsTable`**
   - Idéntica estructura con `Posicion` 1..8 y nombre tabla `DestacadosItems`.

No se necesita migración EF (mismo patrón que `EnsureAroMostrarEnLandingColumn` ya
existente en DbInitializer.cs línea 406). El `AppDbContextModelSnapshot` puede
quedar desincronizado para estas tablas nuevas porque se consultan SOLO desde
repositorios custom (no vía DbSet genérico en OnModelCreating).

## Capa Core (OC.Core)

**Entity nueva**: `OC.Core/Domain/Entities/LandingItem.cs`
```csharp
public class LandingItemCarrusel {
    public int Id { get; set; }
    public byte Posicion { get; set; }  // 1..6
    public string Tipo { get; set; } = "";  // "Producto" | "Aro"
    public int? ProductoId { get; set; }
    public int? AroId { get; set; }
    public Producto? Producto { get; set; }
    public Aro? Aro { get; set; }
}
public class LandingItemDestacado { /* idéntico, Posicion 1..8 */ }
```

**Entity modificada**: `OC.Core/Domain/Entities/Aro.cs`
- Agregar `public string? DescripcionCorta { get; set; }` con `[MaxLength(500)]`

**Interfaces nuevas**:
- `OC.Core/Contracts/IRepositories/ILandingCarruselRepository.cs`
  - `Task<List<LandingItemCarrusel>> GetAllAsync()`
  - `Task ReplaceAllAsync(List<(string Tipo, int Id)> items)` ← borra todas las filas e inserta en una transacción
  - `Task<int> CountAsync()`
- `OC.Core/Contracts/IRepositories/ILandingDestacadoRepository.cs`
  - Misma forma, para la tabla de destacados.

## Capa Data (OC.Data)

**EF Configurations**:
- `OC.Data/Configurations/LandingItemCarruselConfig.cs` (tabla "CarruselItems", FKs, CHECK constraints via `HasCheckConstraint`)
- `OC.Data/Configurations/LandingItemDestacadoConfig.cs` (idem)

**Repositorios**:
- `OC.Data/Repositories/LandingCarruselRepository.cs`
  - `GetAllAsync` con `Include(p => p.Producto).Include(a => a.Aro)`, `AsNoTracking`, ordenado por `Posicion`
  - `ReplaceAllAsync` envuelve `RemoveRange` + `AddRange` en `using var tx = await _context.Database.BeginTransactionAsync()`
- `OC.Data/Repositories/LandingDestacadoRepository.cs` (mismo patrón)

**AppDbContext**: agregar `DbSet<LandingItemCarrusel> CarruselItems` y `DbSet<LandingItemDestacado> DestacadosItems`.

**DbInitializer.cs**: agregar las 3 rutinas nuevas. Llamarlas desde `Program.cs` junto a las existentes.

## Capa Web (OC.Web)

### 1. ArosController.Create / Edit → agregar DescripcionCorta

**Cambios**:
- `Bind` en `Create([Bind(nameof(Aro.Nombre), nameof(Aro.SKU), nameof(Aro.Precio), nameof(Aro.Stock), nameof(Aro.DescripcionCorta), nameof(Aro.MostrarEnLanding))] Aro model, ...)`
- `Edit` idem
- Validación: si `DescripcionCorta` > 500 chars, `ModelState.AddModelError(nameof(Aro.DescripcionCorta), "Máximo 500 caracteres")`

**Views**:
- `Views/Aros/Create.cshtml`: agregar `<div class="mb-3"><label asp-for="DescripcionCorta" class="form-label">Descripción corta</label><textarea asp-for="DescripcionCorta" class="form-control" maxlength="500" rows="2" placeholder="Resumen que verá el cliente en el landing..."></textarea><span asp-validation-for="DescripcionCorta" class="text-danger small"></span></div>`
- `Views/Aros/Edit.cshtml`: idem

### 2. InventoryController.Index → nueva sección "landing"

- Detectar `seccion == "landing"` y cargar la vista parcial
- En el método Index, agregar rama nueva:
  ```csharp
  if (seccion == "landing") {
      var carrusel = await _landingCarruselRepo.GetAllAsync();
      var destacados = await _landingDestacadoRepo.GetAllAsync();
      // productos+aros elegibles (Activo=true). Si Destacado/MostrarEnLanding=true, ya están en slot.
      // Si están marcados pero no están en slot, caen a "Disponibles".
      var elegiblesProductos = (await _productoRepo.GetPagedAsync(1, 200, filter: p => p.Activo)).Items;
      var elegiblesAros = (await _aroRepo.GetPagedAsync(1, 200, filter: a => a.Activo)).Items;
      ViewBag.CarruselItems = carrusel;
      ViewBag.DestacadosItems = destacados;
      ViewBag.ProductosElegibles = elegiblesProductos;
      ViewBag.ArosElegibles = elegiblesAros;
  }
  ```

### 3. InventoryController: nuevos POST endpoints

- `[HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> GuardarCarrusel(List<int> productoIds, List<int> aroIds, List<string> orden)`
  - Recibe: orden es una lista mixta de strings `"P:5"` y `"A:8"` en el orden final deseado
  - Valida: máximo 6 items, tipos únicos, todos los IDs existen
  - **Cascada**: para cada `P:x`, `await _productoRepo.UpdateFlagsAsync(x, Destacado=true)`. Para cada item que sale, `UpdateFlagsAsync(id, false)`.
  - Reemplaza filas de CarruselItems
  - Devuelve `Json(new { ok=true, count=N })`

- `[HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> GuardarDestacados(...)` análogo, máximo 8

- Helper `IProductoRepository.UpdateFlagAsync(int id, bool destacado)` y `IAroRepository.UpdateFlagAsync(int id, bool mostrar)`:
  - Actualizan SOLO el flag, usando `_context.Productos.Update(...)` o query SQL directa
  - Single-property update, evita tocar RutaImagen/otros campos

### 4. Views/Inventory/Index.cshtml → nueva tab

- Agregar `<li class="nav-item">` con icono `bi-collection-play-fill` y label "Landing" (línea ~78 después del tab Aros)
- Agregar `<div class="tab-pane fade @(seccion=="landing" ?"show active" :"")" id="panel-landing">` con:
  - **Subsección Carrusel (6 slots)**: grid de 6 cards fijas (slots), drag-and-drop desde "Disponibles" hacia los slots. Cada slot tiene número grande (#1, #2...) + thumbnail + nombre.
  - **Subsección Destacados (8 slots)**: igual, grid de 8.
  - **Subsección Disponibles (N)**: abajo, lista horizontal scrollable de cards compactas (Producto | Aro) que el admin puede arrastrar a los slots.
- SortableJS CDN (`https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js`).
- Handler `onEnd` lee el nuevo orden del DOM, arma el payload, hace POST a `/Inventory/GuardarCarrusel` o `/Inventory/GuardarDestacados`.
- Al éxito: toast verde "Carrusel guardado (N items)".

### 5. LandingController.Index → usar tablas nuevas

- Cambiar query de carrusel:
  ```csharp
  var carruselDb = await _landingCarruselRepo.GetAllAsync();  // 6 items ya ordenados
  var itemsCarrusel = carruselDb.Select(ci => ci.Tipo == "Producto"
      ? new LandingItemVm { Tipo="Producto", Id=ci.ProductoId, ... ci.Producto }
      : new LandingItemVm { Tipo="Aro", Id=ci.AroId, ... ci.Aro }).ToList();
  ```
- Análogo para destacados
- **Migración de datos**: la primera vez, si CarruselItems está vacía, hacer un seed: tomar los 6 primeros productos destacados + 6 primeros aros con MostrarEnLanding, intercalarlos, insertarlos en CarruselItems con Posicion 1..6. Idem DestacadosItems. Esto evita que el landing se rompa después del deploy.

## Verificación local

1. **Build**: `dotnet build SistemaOpticaComunal.sln -nologo -v quiet` → 0 errores.
2. **Kill proceso viejo**: `pkill -9 -f "OC.Web/bin"` (liberar 5160).
3. **Run**: `dotnet run --project OC.Web --urls "http://localhost:5162"` background (uso 5162 para no chocar con tu 5160).
4. **Wait for "Now listening"** en `process(action='log')`.
5. **DbInitializer corrió**: `grep -E "EnsureCarruselItemsTable|EnsureDestacadosItemsTable|EnsureAroDescripcionCortaColumn"` en el log.
6. **Login admin + ir a /Inventory?seccion=landing**: la pestaña 4 existe y muestra 3 sub-zonas.
7. **Verificar seed automático**: en el primer load, CarruselItems y DestacadosItems se llenaron con los items destacados actuales. Si NO se llenaron, el landing muestra carrusel vacío → bug.
8. **Test drag-and-drop end-to-end**:
   - Arrastrar un Aro de "Disponibles" al slot #2 del Carrusel.
   - Submit (botón "Guardar carrusel").
   - Verificar en BD: `SELECT * FROM CarruselItems WHERE Posicion=2` → muestra Tipo='Aro', AroId=correcto.
   - Verificar cascada: `SELECT MostrarEnLanding FROM Aros WHERE Id=<id>` → debe ser 1.
   - GET /landing/index → el slide #2 muestra ese aro.
9. **Test sacar de slot**:
   - Arrastrar el Aro del slot #2 de vuelta a "Disponibles".
   - Submit.
   - Verificar: `MostrarEnLanding=0` para ese Aro. Slot #2 queda vacío o con el siguiente que entró.
10. **Test DescripcionCorta en Aro**:
    - POST /Aros/Create con DescripcionCorta="Marco dorado vintage, estilo clásico".
    - GET /Aros/Edit/<id> → textarea muestra el valor.
    - En el landing, ese Aro ahora muestra esa descripción en vez del SKU.
11. **Visual**: `browser_navigate http://localhost:5162/Inventory?seccion=landing` + `browser_vision` "se ven los slots claramente, se distinguen Productos de Aros, el área de disponibles es visible".

## Lo que NO voy a hacer

- No tocaré `appsettings*.json`, `run_*.py`, ni `Scripts/` (contienen `Dani4421!`).
- No haré commit ni push. Vos revisás el diff y commitás manualmente.
- No deployaré a producción ni a tu IIS local en 5160 — todo lo verifico en mi proceso en 5162.
- No migraré a una sola tabla ni a JSON — la decisión de dos tablas quedó firme.
- No quitaré `Destacado`/`MostrarEnLanding` de los filtros existentes — siguen siendo fuente de verdad para casos donde el admin quiera marcar un item sin asignarlo a slot (se mantiene la cascada inversa: si desmarca el flag desde Productos/Aros, el item sale del slot automáticamente).

## Decisiones que pueden aparecer durante la implementación

- **UX de slots vacíos**: ¿el slot #3 vacío se muestra como "drop here" con borde punteado? (mi propuesta: sí)
- **Límite de "Disponibles"**: si hay >50 items marcados fuera de slots, ¿hacemos scroll horizontal o paginación? (mi propuesta: scroll horizontal con `overflow-x:auto`)
- **Botón "Limpiar slots"**: ¿agregamos un botón que vacíe TODA una zona? (mi propuesta: no, dejémoslo drag-and-drop puro)

Si querés cambiar algo de esto, decímelo antes de arrancar. Si está bien así, decí **"dale"** y arranco.