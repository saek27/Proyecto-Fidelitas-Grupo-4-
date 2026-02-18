using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Common;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProveedoresController : Controller
    {
        private readonly IGenericRepository<Proveedor> _proveedorRepo;

        public ProveedoresController(IGenericRepository<Proveedor> proveedorRepo)
        {
            _proveedorRepo = proveedorRepo;
        }

        // LISTADO
        public async Task<IActionResult> Index(int page = 1)
        {
            var proveedores = await _proveedorRepo.GetPagedAsync(
                page,
                10,
                p => p.Activo,
                q => q.OrderBy(p => p.Nombre)
            );

            return View(proveedores);
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Activo = true;

            await _proveedorRepo.AddAsync(model);

            TempData["Success"] = "Proveedor registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _proveedorRepo.GetByIdAsync(id);

            if (proveedor == null)
                return NotFound();

            return View(proveedor);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedor model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _proveedorRepo.UpdateAsync(model);

            TempData["Success"] = "Proveedor actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // DESACTIVAR (Soft Delete)
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var proveedor = await _proveedorRepo.GetByIdAsync(id);

            if (proveedor == null)
                return NotFound();

            proveedor.Activo = !proveedor.Activo;

            await _proveedorRepo.UpdateAsync(proveedor);

            return RedirectToAction(nameof(Index));
        }
    }
}
