using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Common;
using OC.Core.Domain.Entities;


namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PedidosController : Controller
    {
        private readonly IGenericRepository<Pedido> _pedidoRepo;
        private readonly IGenericRepository<Proveedor> _proveedorRepo;

        public PedidosController(
            IGenericRepository<Pedido> pedidoRepo,
            IGenericRepository<Proveedor> proveedorRepo)
        {
            _pedidoRepo = pedidoRepo;
            _proveedorRepo = proveedorRepo;
        }

        // HISTORIAL
        public async Task<IActionResult> Historial(int page = 1)
        {
            var pedidos = await _pedidoRepo.GetPagedAsync(
                page,
                10,
                p => p.Activo,
                q => q.OrderByDescending(p => p.FechaPedido),
                "Proveedor"
            );

            return View(pedidos);
        }

        // DETALLE
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }


        // GET CREATE
        public async Task<IActionResult> Create()
        {
            var proveedores = await _proveedorRepo.GetPagedAsync(
                1,
                100,
                p => p.Activo
            );

            ViewBag.Proveedores = proveedores.Items;

            return View();
        }

        // POST CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(Pedido pedido)
        {
            if (!ModelState.IsValid)
            {
                var proveedores = await _proveedorRepo.GetPagedAsync(
                    1,
                    100,
                    p => p.Activo
                );

                ViewBag.Proveedores = proveedores.Items;
                return View(pedido);
            }

            pedido.FechaPedido = DateTime.Now;
            pedido.Estado = EstadoPedido.Pendiente;
            pedido.Activo = true;

            await _pedidoRepo.AddAsync(pedido);

            return RedirectToAction(nameof(Historial));
        }
        //EDIT
        public async Task<IActionResult> Edit(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);

            if (pedido == null)
                return NotFound();

            var proveedores = await _proveedorRepo.GetPagedAsync(
                1,
                100,
                p => p.Activo
            );

            ViewBag.Proveedores = proveedores.Items;

            return View(pedido);
        }

        //POST EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pedido model)
        {
            if (!ModelState.IsValid)
            {
                var proveedores = await _proveedorRepo.GetPagedAsync(1, 100, p => p.Activo);
                ViewBag.Proveedores = proveedores.Items;
                return View(model);
            }

            var pedido = await _pedidoRepo.GetByIdAsync(id);

            if (pedido == null)
                return NotFound();

            pedido.ProveedorId = model.ProveedorId;
            pedido.Descripcion = model.Descripcion;
            pedido.FechaEntregaEstimada = model.FechaEntregaEstimada;

            // 🔥 Delegamos al dominio
            pedido.CambiarEstado(model.Estado);

            await _pedidoRepo.UpdateAsync(pedido);

            return RedirectToAction(nameof(Historial));
        }


        //SOFFT DELETE

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);

            if (pedido == null)
                return NotFound();

            pedido.Activo = false;

            await _pedidoRepo.UpdateAsync(pedido);

            return RedirectToAction(nameof(Historial));
        }



    }




}

