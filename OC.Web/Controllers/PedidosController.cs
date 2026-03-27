using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PedidosController : Controller
    {
        private readonly IGenericRepository<Pedido> _pedidoRepo;
        private readonly IGenericRepository<Proveedor> _proveedorRepo;
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<DetallePedido> _detalleRepo;
        private readonly AppDbContext _context; // Inyectamos el contexto para transacciones

        public PedidosController(
            IGenericRepository<Pedido> pedidoRepo,
            IGenericRepository<Proveedor> proveedorRepo,
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<DetallePedido> detalleRepo,
            AppDbContext context) // Nuevo parámetro
        {
            _pedidoRepo = pedidoRepo;
            _proveedorRepo = proveedorRepo;
            _productoRepo = productoRepo;
            _detalleRepo = detalleRepo;
            _context = context;
        }

        // GET: Pedidos/Historial
        public async Task<IActionResult> Historial(int page = 1)
        {
            var pedidos = await _pedidoRepo.GetPagedAsync(
                page,
                10,
                filter: p => p.Activo,
                orderBy: q => q.OrderByDescending(p => p.FechaPedido),
                includeProperties: "Proveedor"
            );
            return View(pedidos);
        }

        // GET: Pedidos/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new PedidoCreateViewModel
            {
                Proveedores = (await _proveedorRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                    .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre }),
                Productos = (await _productoRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                    .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.Nombre} (SKU: {p.SKU})" })
            };
            return View(viewModel);
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidoCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasCreate(model);
                return View(model);
            }

            // Iniciamos una transacción explícita
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = new Pedido
                {
                    ProveedorId = model.ProveedorId,
                    FechaPedido = DateTime.Now,
                    FechaEntregaEstimada = model.FechaEntregaEstimada,
                    Descripcion = model.Descripcion ?? "",
                    Estado = EstadoPedido.Pendiente,
                    Activo = true
                };

                await _pedidoRepo.AddAsync(pedido); // Esto ya hace SaveChanges, pero está dentro de la transacción

                foreach (var detalleVm in model.Detalles)
                {
                    var producto = await _productoRepo.GetByIdAsync(detalleVm.ProductoId);
                    if (producto == null) continue;

                    var detalle = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = detalleVm.ProductoId,
                        Cantidad = detalleVm.Cantidad,
                        CostoUnitario = producto.CostoUnitario
                    };
                    await _detalleRepo.AddAsync(detalle);
                }

                await transaction.CommitAsync();
                TempData["Success"] = "Pedido creado correctamente.";
                return RedirectToAction(nameof(Historial));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Loguear el error
                ModelState.AddModelError("", "Ocurrió un error al crear el pedido. Intente nuevamente.");
                await CargarListasCreate(model);
                return View(model);
            }
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var pedido = await _pedidoRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id && p.Activo,
                includeProperties: "Proveedor,Detalles.Producto"
            );
            var pedidoEntity = pedido.Items.FirstOrDefault();
            if (pedidoEntity == null) return NotFound();

            var viewModel = new PedidoEditViewModel
            {
                Id = pedidoEntity.Id,
                ProveedorId = pedidoEntity.ProveedorId,
                FechaEntregaEstimada = pedidoEntity.FechaEntregaEstimada,
                Descripcion = pedidoEntity.Descripcion,
                Estado = pedidoEntity.Estado,
                Detalles = pedidoEntity.Detalles?.Select(d => new DetallePedidoViewModel
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    NombreProducto = d.Producto?.Nombre,
                    CostoUnitario = d.CostoUnitario
                }).ToList() ?? new List<DetallePedidoViewModel>(),
                Proveedores = (await _proveedorRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                    .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre, Selected = p.Id == pedidoEntity.ProveedorId }),
                Productos = (await _productoRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                    .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.Nombre} (SKU: {p.SKU})" })
            };

            return View(viewModel);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PedidoEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListas(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cargar el pedido con detalles desde la BD (usando el contexto directamente para asegurar tracking)
                var pedido = await _context.Pedidos
                    .Include(p => p.Detalles)
                    .FirstOrDefaultAsync(p => p.Id == model.Id && p.Activo);

                if (pedido == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                var estadoAnterior = pedido.Estado;

                // Actualizar propiedades
                pedido.ProveedorId = model.ProveedorId;
                pedido.FechaEntregaEstimada = model.FechaEntregaEstimada;
                pedido.Descripcion = model.Descripcion ?? "";
                pedido.CambiarEstado(model.Estado);

                // Eliminar detalles antiguos (usando el contexto)
                if (pedido.Detalles != null && pedido.Detalles.Any())
                {
                    _context.DetallePedidos.RemoveRange(pedido.Detalles);
                }

                // Agregar nuevos detalles
                foreach (var detalleVm in model.Detalles)
                {
                    var producto = await _productoRepo.GetByIdAsync(detalleVm.ProductoId);
                    if (producto == null) continue;

                    var detalle = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = detalleVm.ProductoId,
                        Cantidad = detalleVm.Cantidad,
                        CostoUnitario = producto.CostoUnitario
                    };
                    _context.DetallePedidos.Add(detalle);
                }

                // Si el estado cambió a Recibido, actualizar stock (también con el contexto)
                if (estadoAnterior != EstadoPedido.Recibido && pedido.Estado == EstadoPedido.Recibido)
                {
                    foreach (var detalleVm in model.Detalles)
                    {
                        var producto = await _productoRepo.GetByIdAsync(detalleVm.ProductoId);
                        if (producto != null)
                        {
                            producto.Stock += detalleVm.Cantidad;
                            _context.Productos.Update(producto);
                        }
                    }
                }

                // Guardar todos los cambios en una sola operación
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Pedido actualizado correctamente.";
                return RedirectToAction(nameof(Details), new { id = pedido.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "El pedido fue modificado o eliminado por otro usuario. Por favor, recargue la página.");
                await CargarListas(model);
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Loguear excepción
                ModelState.AddModelError("", "Ocurrió un error al actualizar el pedido. Intente nuevamente.");
                await CargarListas(model);
                return View(model);
            }
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _pedidoRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id && p.Activo,
                includeProperties: "Proveedor,Detalles.Producto"
            );
            var pedidoEntity = pedido.Items.FirstOrDefault();
            if (pedidoEntity == null) return NotFound();

            return View(pedidoEntity);
        }

        // POST: Pedidos/Desactivar/5 (soft delete)
        [HttpPost]
        public async Task<IActionResult> Desactivar(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);
            if (pedido == null) return NotFound();

            pedido.Activo = false;
            await _pedidoRepo.UpdateAsync(pedido);

            return RedirectToAction(nameof(Historial));
        }

        private async Task CargarListasCreate(PedidoCreateViewModel model)
        {
            model.Proveedores = (await _proveedorRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre });
            model.Productos = (await _productoRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.Nombre} (SKU: {p.SKU})" });
        }

        private async Task CargarListas(PedidoEditViewModel model)
        {
            model.Proveedores = (await _proveedorRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre });
            model.Productos = (await _productoRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                .Items.Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.Nombre} (SKU: {p.SKU})" });
        }

        // GET: Pedidos/Index (página principal de pedidos)
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var result = await _pedidoRepo.GetPagedAsync(
                page,
                pageSize,
                filter: p => p.Activo,
                orderBy: q => q.OrderByDescending(p => p.FechaPedido),
                includeProperties: "Proveedor"
            );

            return View(result);
        }
    }
}