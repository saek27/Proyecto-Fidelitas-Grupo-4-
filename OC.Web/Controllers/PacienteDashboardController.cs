using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Paciente")]
    public class PacienteDashboardController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;

        public PacienteDashboardController(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Cita> citasRepo,
            IGenericRepository<SolicitudCita> solicitudesRepo)
        {
            _pacientesRepo = pacientesRepo;
            _citasRepo = citasRepo;
            _solicitudesRepo = solicitudesRepo;
        }

        public async Task<IActionResult> Index()
        {
            // Obtener el ID del paciente desde los Claims
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
            {
                return RedirectToAction("Login", "PacienteAccount");
            }

            // Obtener información del paciente
            var paciente = await _pacientesRepo.GetByIdAsync(pacienteId);
            if (paciente == null)
            {
                return RedirectToAction("Login", "PacienteAccount");
            }

            // Obtener citas del paciente
            try
            {
                var citas = await _citasRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 100,
                    filter: c => c.PacienteId == pacienteId,
                    orderBy: q => q.OrderByDescending(c => c.FechaHora),
                    includeProperties: "Paciente"
                );

                ViewBag.Paciente = paciente;
                ViewBag.Citas = citas.Items;
            }
            catch
            {
                // Si la tabla no existe aún, mostrar lista vacía
                ViewBag.Paciente = paciente;
                ViewBag.Citas = new List<Cita>();
            }

            // Obtener solicitudes pendientes del paciente
            try
            {
                var solicitudes = await _solicitudesRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 100,
                    filter: s => s.PacienteId == pacienteId && s.Estado == "Pendiente",
                    orderBy: q => q.OrderByDescending(s => s.FechaSolicitud)
                );

                ViewBag.SolicitudesPendientes = solicitudes.Items;
            }
            catch
            {
                ViewBag.SolicitudesPendientes = new List<SolicitudCita>();
            }

            return View();
        }

        [HttpGet]
        public IActionResult SolicitarCita()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarCita(string motivo)
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
            {
                return RedirectToAction("Login", "PacienteAccount");
            }

            try
            {
                var solicitud = new SolicitudCita
                {
                    PacienteId = pacienteId,
                    Motivo = motivo,
                    FechaSolicitud = DateTime.Now,
                    Estado = "Pendiente"
                };

                await _solicitudesRepo.AddAsync(solicitud);

                TempData["Success"] = "Solicitud de cita enviada exitosamente. Un recepcionista se pondrá en contacto con usted.";
            }
            catch
            {
                TempData["Error"] = "Error al enviar la solicitud. Por favor, intente más tarde.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
