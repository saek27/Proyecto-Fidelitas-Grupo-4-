using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Recepcion,Admin")]
    public class SolicitudesCitasController : Controller
    {
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;
        private readonly IGenericRepository<Usuario> _usuariosRepo;

        public SolicitudesCitasController(
            IGenericRepository<SolicitudCita> solicitudesRepo,
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Cita> citasRepo,
            IGenericRepository<Usuario> usuariosRepo)
        {
            _solicitudesRepo = solicitudesRepo;
            _pacientesRepo = pacientesRepo;
            _citasRepo = citasRepo;
            _usuariosRepo = usuariosRepo;
        }

        // LISTAR SOLICITUDES PENDIENTES
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _solicitudesRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: 10,
                filter: s => s.Estado == "Pendiente",
                orderBy: q => q.OrderByDescending(s => s.FechaSolicitud),
                includeProperties: "Paciente"
            );

            // Contar solicitudes pendientes para notificaciones
            var pendientes = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1000,
                filter: s => s.Estado == "Pendiente"
            );
            ViewBag.PendientesCount = pendientes.TotalCount;

            return View(result);
        }

        // AGENDAR CITA (Convertir solicitud en cita)
        [HttpGet]
        public async Task<IActionResult> Agendar(int id)
        {
            var solicitud = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: s => s.Id == id,
                includeProperties: "Paciente"
            );

            var solicitudEntity = solicitud.Items.FirstOrDefault();
            if (solicitudEntity == null) return NotFound();

            // Cargar optometristas para el dropdown
            var todosUsuarios = await _usuariosRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1000,
                filter: u => u.Activo,
                includeProperties: "Rol"
            );

            var optometristas = todosUsuarios.Items.Where(u => u.Rol.Nombre == "Optometrista");

            var model = new CitaViewModel
            {
                SolicitudCitaId = solicitudEntity.Id,
                PacienteId = solicitudEntity.PacienteId,
                NombrePaciente = solicitudEntity.Paciente.NombreCompleto,
                MotivoSolicitud = solicitudEntity.Motivo,
                FechaHora = DateTime.Now.AddDays(1),
                OptometristasList = optometristas.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Nombre
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agendar(CitaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar dropdowns
                var todosUsuarios2 = await _usuariosRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 1000,
                    filter: u => u.Activo,
                    includeProperties: "Rol"
                );

                var optometristas2 = todosUsuarios2.Items.Where(u => u.Rol.Nombre == "Optometrista");

                model.OptometristasList = optometristas2.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Nombre
                }).ToList();

                return View(model);
            }

            // Obtener el ID del usuario actual (recepcionista)
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int? usuarioAprobadorId = null;
            if (int.TryParse(userIdClaim, out int userId))
            {
                usuarioAprobadorId = userId;
            }

            // Actualizar la solicitud a "Aprobada"
            var solicitud = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: s => s.Id == model.SolicitudCitaId
            );

            var solicitudEntity = solicitud.Items.FirstOrDefault();
            if (solicitudEntity == null) return NotFound();

            solicitudEntity.Estado = "Aprobada";
            solicitudEntity.FechaAprobacion = DateTime.Now;
            solicitudEntity.UsuarioAprobadorId = usuarioAprobadorId;
            await _solicitudesRepo.UpdateAsync(solicitudEntity);

            // Crear la cita
            var cita = new Cita
            {
                PacienteId = model.PacienteId,
                SolicitudCitaId = model.SolicitudCitaId,
                FechaHora = model.FechaHora,
                Observaciones = model.Observaciones,
                Estado = "Programada",
                UsuarioAsignadoId = model.UsuarioAsignadoId,
                FechaCreacion = DateTime.Now
            };

            await _citasRepo.AddAsync(cita);

            TempData["Success"] = "Cita agendada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // RECHAZAR SOLICITUD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: s => s.Id == id
            );

            var solicitudEntity = solicitud.Items.FirstOrDefault();
            if (solicitudEntity == null) return NotFound();

            solicitudEntity.Estado = "Rechazada";
            solicitudEntity.FechaAprobacion = DateTime.Now;
            await _solicitudesRepo.UpdateAsync(solicitudEntity);

            TempData["Success"] = "Solicitud rechazada";
            return RedirectToAction(nameof(Index));
        }
    }
}
