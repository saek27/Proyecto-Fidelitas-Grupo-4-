using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Common;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IGenericRepository<Producto> _productoRepo;

        public InventoryController(IGenericRepository<Producto> productoRepo)
        {
            _productoRepo = productoRepo;
        }

        // Lista solo productos activos (catálogo disponible)
        /*
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _productoRepo.GetPagedAsync(
                page,
                10,
                p => p.Activo,
                q => q.OrderBy(p => p.Nombre)
            );
            return View(result);
        }
        */


        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var result = await _productoRepo.GetPagedAsync(
                page,
                pageSize,
                p => p.Activo,
                q => q.OrderBy(p => p.Nombre)
            );

            // 🔻 Productos con bajo stock
            var lowStock = await _productoRepo.GetAllAsync(
                p => p.Activo && p.Stock < 6
            );

            ViewBag.PageSize = pageSize;

            ViewBag.LowStock = lowStock;

            return View(result);
        }
        public IActionResult Create()
        {
            return View(new Producto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto model)
        {
            if (string.IsNullOrWhiteSpace(model.SKU))
            {
                ModelState.AddModelError("SKU", "El SKU es requerido.");
                return View(model);
            }
            var skuNorm = model.SKU.Trim().ToUpperInvariant();
            var existente = await _productoRepo.GetPagedAsync(1, 1, p => p.SKU == skuNorm);
            if (existente.TotalCount > 0)
            {
                ModelState.AddModelError("SKU", "Ya existe un producto con este SKU.");
                return View(model);
            }
            if (!ModelState.IsValid)
                return View(model);

            model.SKU = skuNorm;
            model.Activo = true;
            await _productoRepo.AddAsync(model);

            TempData["Success"] = "Producto registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Producto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existente = await _productoRepo.GetByIdAsync(model.Id);
            if (existente == null)
                return NotFound();

            existente.Nombre = model.Nombre;
            existente.CostoUnitario = model.CostoUnitario;
            existente.Stock = model.Stock;
            await _productoRepo.UpdateAsync(existente);

            TempData["Success"] = "Costo y stock actualizados correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // Eliminar del catálogo activo (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();

            producto.Activo = false;
            await _productoRepo.UpdateAsync(producto);

            TempData["Success"] = "Producto eliminado del catálogo.";
            return RedirectToAction(nameof(Index));
        }
    }
}
