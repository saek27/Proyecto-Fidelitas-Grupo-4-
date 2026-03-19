using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Services;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Recepcion,Optometrista")]
    public class OrdenesTrabajoController : Controller
    {
        private readonly IGenericRepository<OrdenTrabajo> _ordenesRepo;
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Sucursal> _sucursalesRepo;
        private readonly IGenericRepository<Venta> _ventasRepo;
        private readonly INotificationService _notificationService;

        public OrdenesTrabajoController(
            IGenericRepository<OrdenTrabajo> ordenesRepo,
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Sucursal> sucursalesRepo,
            IGenericRepository<Venta> ventasRepo,
            INotificationService notificationService)
        {
            _ordenesRepo = ordenesRepo;
            _pacientesRepo = pacientesRepo;
            _sucursalesRepo = sucursalesRepo;
            _ventasRepo = ventasRepo;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _ordenesRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: 10,
                orderBy: q => q.OrderByDescending(o => o.FechaCreacion),
                includeProperties: "Paciente,Sucursal"
            );
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await RecargarViewBag();
            return View(new OrdenTrabajoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrdenTrabajoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RecargarViewBag();
                return View(model);
            }

            var orden = new OrdenTrabajo
            {
                PacienteId = model.PacienteId,
                SucursalId = model.SucursalId,
                VentaId = model.VentaId,
                Referencia = model.Referencia,
                Estado = EstadoOrdenTrabajo.Pendiente,
                FechaCreacion = DateTime.Now
            };
            await _ordenesRepo.AddAsync(orden);

            TempData["Success"] = "Orden de trabajo creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var orden = await _ordenesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: o => o.Id == id,
                includeProperties: "Paciente,Sucursal"
            );
            var entity = orden.Items.FirstOrDefault();
            if (entity == null) return NotFound();

            ViewBag.Estados = new SelectList(new[]
            {
                new { Value = EstadoOrdenTrabajo.Pendiente, Text = "Pendiente" },
                new { Value = EstadoOrdenTrabajo.EnProceso, Text = "En proceso" },
                new { Value = EstadoOrdenTrabajo.Lista, Text = "Lista" },
                new { Value = EstadoOrdenTrabajo.Entregada, Text = "Entregada" }
            }, "Value", "Text", entity.Estado);

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string estado)
        {
            var ordenResult = await _ordenesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: o => o.Id == id,
                includeProperties: "Paciente,Sucursal"
            );
            var orden = ordenResult.Items.FirstOrDefault();
            if (orden == null) return NotFound();

            var estadoAnterior = orden.Estado;
            orden.Estado = estado ?? orden.Estado;
            if (orden.Estado == EstadoOrdenTrabajo.Lista && !orden.FechaLista.HasValue)
                orden.FechaLista = DateTime.Now;

            await _ordenesRepo.UpdateAsync(orden);

            if (orden.Estado == EstadoOrdenTrabajo.Lista && estadoAnterior != EstadoOrdenTrabajo.Lista)
            {
                var enviado = await _notificationService.NotificarLentesListosAsync(orden);
                if (!enviado)
                {
                    TempData["Error"] = "El estado se actualizó a 'Lista', pero no se pudo notificar al paciente: no tiene correo ni teléfono registrado. Registre los datos de contacto del paciente para que reciba la notificación.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            TempData["Success"] = "Orden de trabajo actualizada correctamente." + (orden.Estado == EstadoOrdenTrabajo.Lista ? " Se ha notificado al paciente." : "");
            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarViewBag()
        {
            var pacientes = await _pacientesRepo.GetPagedAsync(1, 500, orderBy: q => q.OrderBy(p => p.Nombres));
            var sucursales = await _sucursalesRepo.GetPagedAsync(1, 100, filter: s => s.Activo);
            var ventas = await _ventasRepo.GetPagedAsync(1, 200, orderBy: q => q.OrderByDescending(v => v.FechaVenta), includeProperties: "Paciente");

            ViewBag.Pacientes = new SelectList(pacientes.Items.Select(p => new { p.Id, Nombre = p.NombreCompleto + " (" + p.Cedula + ")" }), "Id", "Nombre");
            ViewBag.Sucursales = new SelectList(sucursales.Items, "Id", "Nombre");
            ViewBag.Ventas = new SelectList(
                ventas.Items.Select(v => new { v.Id, Text = v.NumeroFactura + " - " + (v.Paciente?.NombreCompleto ?? "") }),
                "Id", "Text");
        }
    }
}
