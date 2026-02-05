using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmpleadosController : Controller
    {
        private readonly IGenericRepository<Empleado> _empleadosRepo;
        private readonly IGenericRepository<Sucursal> _sucursalesRepo;

        public EmpleadosController(
            IGenericRepository<Empleado> empleadosRepo,
            IGenericRepository<Sucursal> sucursalesRepo)
        {
            _empleadosRepo = empleadosRepo;
            _sucursalesRepo = sucursalesRepo;
        }

        // LISTAR
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _empleadosRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: 10,
                filter: e => e.Activo,
                includeProperties: "Sucursal"
            );

            return View(result);
        }

        // CREATE
        public async Task<IActionResult> Create()
        {
            ViewBag.Sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1,
            pageSize: 1000, filter: s => s.Activo);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpleadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1,
            pageSize: 1000, filter: s => s.Activo);
                return View(model);
            }

            var entity = new Empleado
            {
                Nombre = model.Nombre,
                Apellidos = model.Apellidos,
                Cedula = model.Cedula,
                Telefono = model.Telefono,
                Puesto = model.Puesto,
                SucursalId = model.SucursalId,
                Activo = true
            };

            await _empleadosRepo.AddAsync(entity);

            TempData["Success"] = "Empleado creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // EDIT
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _empleadosRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            ViewBag.Sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1,
            pageSize: 1000, filter: s => s.Activo);

            return View(new EmpleadoViewModel
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Apellidos = entity.Apellidos,
                Cedula = entity.Cedula,
                Telefono = entity.Telefono,
                Puesto = entity.Puesto,
                SucursalId = entity.SucursalId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmpleadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1, pageSize: 1000, filter: s => s.Activo);
                return View(model);
            }

            var entity = await _empleadosRepo.GetByIdAsync(model.Id);
            if (entity == null) return NotFound();

            entity.Nombre = model.Nombre;
            entity.Apellidos = model.Apellidos;
            entity.Cedula = model.Cedula;
            entity.Telefono = model.Telefono;
            entity.Puesto = model.Puesto;
            entity.SucursalId = model.SucursalId;

            await _empleadosRepo.UpdateAsync(entity);

            TempData["Success"] = "Empleado actualizado";
            return RedirectToAction(nameof(Index));
        }

        // SOFT DELETE
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var entity = await _empleadosRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Activo = !entity.Activo;
            await _empleadosRepo.UpdateAsync(entity);

            return RedirectToAction(nameof(Index));
        }
    }
}
