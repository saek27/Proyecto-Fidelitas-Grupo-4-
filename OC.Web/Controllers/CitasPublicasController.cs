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
            {
                resultado = resultado.Where(c => c.Estado == estado);
            }

            return View(resultado.ToList());
        }


        [Authorize(Roles = "Optometrista, Recepcion, Admin")]
        public async Task<IActionResult> Editar(int id)
        {
            var cita = (await _citasRepo.GetPagedAsync(
                1, 1, c => c.Id == id,
                includeProperties: "Paciente")).Items.FirstOrDefault();

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
                c => c.Id == id,
                includeProperties: "Paciente"
            )).Items.FirstOrDefault();

            if (esRecepcionista && estado != "Cancelada")
            {
                ModelState.AddModelError("", "Recepción solo puede cancelar citas.");
                return View(cita);
            }

            if (cita == null) return NotFound();


            // Validación clínica
            if (estado == "Atendida" && string.IsNullOrWhiteSpace(resultadoClinico))
            {
                ModelState.AddModelError("", "Debe ingresar resultado clínico.");
                return View(cita);
            }

            cita.Estado = estado;

            if (estado == "Atendida")
            {
                cita.ObservacionesEspecialista = resultadoClinico;
                cita.FechaCreacion = DateTime.Now;
            }

            await _citasRepo.UpdateAsync(cita);

            return RedirectToAction("CitasPaciente", "CitasPublicas");

        }

        [Authorize]
        public async Task<IActionResult> MiHistorial()
        {
            var userCedula = User.FindFirst("Cedula")?.Value;

            var paciente = (await _pacientesRepo.GetPagedAsync(
                1, 1,
                p => p.Cedula == userCedula
            )).Items.FirstOrDefault();

            if (paciente == null)
                return RedirectToAction("Index", "Home");

            var citas = await _citasRepo.GetPagedAsync(
                1, 200,
                c => c.PacienteId == paciente.Id && c.Estado == "Atendida",
                q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Paciente"
            );

            return View(citas.Items);
        }





    }
}
