using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Helpers;
using OC.Web.ViewModels;
using OC.Web.Services;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [AllowAnonymous]
    public class CitasPublicasController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;
        private readonly IGenericRepository<Expediente> _expedienteRepo;
        private readonly INotificationService _notificationService;

        public CitasPublicasController(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<SolicitudCita> solicitudesRepo,
            IGenericRepository<Cita> citasRepo,
            IGenericRepository<Expediente> expedienteRepo,
            INotificationService notificationService)
        {
            _pacientesRepo = pacientesRepo;
            _solicitudesRepo = solicitudesRepo;
            _citasRepo = citasRepo;
            _expedienteRepo = expedienteRepo;
            _notificationService = notificationService;
        }

        // SOLICITAR CITA (Público)
        [HttpGet]
        public IActionResult Solicitar() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(string cedula, string motivo)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View();
            }

            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm
            );
            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null)
            {
                ModelState.AddModelError("", "No se encontró un paciente con esa cédula. Por favor, regístrese primero.");
                return View();
            }

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
        public IActionResult VerMisCitas() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerMisCitas(string cedula)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View();
            }

            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm,
                includeProperties: "Citas"
            );
            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null)
            {
                ModelState.AddModelError("", "No se encontró un paciente con esa cédula.");
                return View();
            }

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

        [Authorize]
        public async Task<IActionResult> CitasPaciente(string estado)
        {
            var citas = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 200,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Paciente"
            );

            var resultado = citas.Items.AsQueryable();
            if (!string.IsNullOrEmpty(estado))
                resultado = resultado.Where(c => c.Estado == estado);

            return View(resultado.ToList());
        }

        [Authorize(Roles = "Optometrista, Recepcion, Admin")]
        public async Task<IActionResult> Editar(int id)
        {
            var cita = (await _citasRepo.GetPagedAsync(
                1, 1,
                filter: c => c.Id == id,
                includeProperties: "Paciente"
            )).Items.FirstOrDefault();

            if (cita == null) return NotFound();
            return View(cita);
        }

        [HttpPost]
        [Authorize(Roles = "Optometrista, Recepcion, Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string estado, string resultadoClinico)
        {
            var esRecepcionista = User.IsInRole("Recepcion");
            var cita = (await _citasRepo.GetPagedAsync(
                1, 1,
                filter: c => c.Id == id,
                includeProperties: "Paciente"
            )).Items.FirstOrDefault();

            if (esRecepcionista && estado != EstadoCita.Cancelada)
            {
                ModelState.AddModelError("", "Recepción solo puede cancelar citas.");
                return View(cita);
            }

            if (cita == null) return NotFound();

            if (estado == EstadoCita.Atendida && string.IsNullOrWhiteSpace(resultadoClinico))
            {
                ModelState.AddModelError("", "Debe ingresar resultado clínico.");
                return View(cita);
            }

            cita.Estado = estado;
            if (estado == EstadoCita.Atendida)
            {
                cita.ObservacionesEspecialista = resultadoClinico;
            }

            await _citasRepo.UpdateAsync(cita);

            // Escenario 3 CIT-RF-016: notificar cancelación al paciente
            if (estado == EstadoCita.Cancelada)
                await _notificationService.EnviarNotificacionCancelacionAsync(cita);

            TempData["Success"] = "Cita actualizada correctamente.";

            if (estado == EstadoCita.Atendida && (User.IsInRole("Optometrista") || User.IsInRole("Admin")))
            {
                return RedirectToAction("Create", "Expedientess", new { citaId = cita.Id });
            }
            else
            {
                return RedirectToAction("CitasPaciente");
            }
        }

        [Authorize]
        public async Task<IActionResult> MiHistorial()
        {
            var userCedula = User.FindFirst("Cedula")?.Value;
            var paciente = (await _pacientesRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Cedula == userCedula
            )).Items.FirstOrDefault();

            if (paciente == null)
                return RedirectToAction("Index", "Home");

            var citas = await _citasRepo.GetPagedAsync(
                1, 200,
                filter: c => c.PacienteId == paciente.Id && c.Estado == EstadoCita.Atendida,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Paciente"
            );
            return View(citas.Items);
        }

        // NUEVA ACCIÓN: Historial de expedientes (misma URL, pero ahora devuelve expedientes)
        [Authorize(Roles = "Optometrista,Admin")]
        public async Task<IActionResult> HistorialPaciente(int pacienteId)
        {
            var expedientes = await _expedienteRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: e => e.Cita.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(e => e.FechaRegistro),
                includeProperties: "Cita.Paciente,ValoresClinicos,Documentos"
            );

            if (!expedientes.Items.Any())
            {
                TempData["Info"] = "Este paciente no tiene expedientes registrados.";
            }

            return View("~/Views/Expedientess/HistorialPaciente.cshtml", expedientes.Items);
        }
    }
}
