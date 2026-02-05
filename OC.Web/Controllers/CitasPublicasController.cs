using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    // Controlador público para que los pacientes soliciten citas
    [AllowAnonymous]
    public class CitasPublicasController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;

        public CitasPublicasController(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<SolicitudCita> solicitudesRepo,
            IGenericRepository<Cita> citasRepo)
        {
            _pacientesRepo = pacientesRepo;
            _solicitudesRepo = solicitudesRepo;
            _citasRepo = citasRepo;
        }

        // SOLICITAR CITA (Público)
        [HttpGet]
        public IActionResult Solicitar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(string cedula, string motivo)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                ModelState.AddModelError("", "Debe ingresar su cédula.");
                return View();
            }

            // Buscar paciente por cédula
            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedula
            );

            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null)
            {
                ModelState.AddModelError("", "No se encontró un paciente con esa cédula. Por favor, regístrese primero.");
                return View();
            }

            // Crear la solicitud de cita
            var solicitud = new SolicitudCita
            {
                PacienteId = paciente.Id,
                Motivo = motivo,
                FechaSolicitud = DateTime.Now,
                Estado = "Pendiente"
            };

            await _solicitudesRepo.AddAsync(solicitud);

            TempData["Success"] = "Solicitud de cita enviada exitosamente. Un recepcionista se pondrá en contacto con usted.";
            return RedirectToAction(nameof(Solicitar));
        }

        // VER MIS CITAS (Público - por cédula)
        [HttpGet]
        public IActionResult VerMisCitas()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerMisCitas(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                ModelState.AddModelError("", "Debe ingresar su cédula.");
                return View();
            }

            // Buscar paciente por cédula
            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedula,
                includeProperties: "Citas"
            );

            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null)
            {
                ModelState.AddModelError("", "No se encontró un paciente con esa cédula.");
                return View();
            }

            // Obtener citas del paciente
            var citas = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: c => c.PacienteId == paciente.Id,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Paciente"
            );

            ViewBag.Paciente = paciente;
            ViewBag.Citas = citas.Items;

            return View();
        }
    }
}
