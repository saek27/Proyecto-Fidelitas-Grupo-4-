using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly IGenericRepository<Ticket> _ticketRepo;
        private readonly IGenericRepository<Equipo> _equipoRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;

        public TicketsController(
            IGenericRepository<Ticket> ticketRepo,
            IGenericRepository<Equipo> equipoRepo,
            IGenericRepository<Usuario> usuarioRepo)
        {
            _ticketRepo = ticketRepo;
            _equipoRepo = equipoRepo;
            _usuarioRepo = usuarioRepo;
        }

        #region Acciones para todos los usuarios (creación y consulta propia)

        // GET: Tickets/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new TicketCreateViewModel
            {
                EquiposList = await ObtenerEquipos()
            };
            return View(viewModel);
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.EquiposList = await ObtenerEquipos();
                return View(model);
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            // Generar número de seguimiento único
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var ultimoTicket = (await _ticketRepo.GetPagedAsync(1, 1, orderBy: q => q.OrderByDescending(t => t.Id)))
                .Items.FirstOrDefault();
            int nuevoNum = (ultimoTicket?.Id ?? 0) + 1;
            var numeroSeguimiento = $"TKT-{fecha}-{nuevoNum:D4}";

            var ticket = new Ticket
            {
                NumeroSeguimiento = numeroSeguimiento,
                Titulo = model.Titulo,
                Descripcion = model.Descripcion,
                Estado = "Pendiente",          // Estado inicial
                CreadoPorId = userId,
                EquipoId = model.EquipoId,
                FechaCreacion = DateTime.Now
                // Prioridad y Tipo se asignan después por técnico/admin
            };

            await _ticketRepo.AddAsync(ticket);

            TempData["Success"] = $"Ticket {numeroSeguimiento} creado exitosamente.";
            return RedirectToAction(nameof(MisTickets));
        }

        // GET: Tickets/MisTickets
        public async Task<IActionResult> MisTickets(int page = 1)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            var tickets = await _ticketRepo.GetPagedAsync(
                page, 10,
                filter: t => t.CreadoPorId == userId,
                orderBy: q => q.OrderByDescending(t => t.FechaCreacion),
                includeProperties: "Equipo,TecnicoAsignado"
            );

            return View(tickets);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo,TecnicoAsignado"
            )).Items.FirstOrDefault();

            if (ticket == null)
                return NotFound();

            return View(ticket);
        }

        #endregion

        #region Acciones para Técnico y Admin (panel técnico, clasificar, cerrar, tomar)

        // GET: Tickets/PanelTecnico
        [Authorize(Roles = "Tecnico,Admin")]
        public async Task<IActionResult> PanelTecnico(int page = 1)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int userId);

            // Tickets sin asignar O asignados al técnico actual (excluye cerrados)
            var tickets = await _ticketRepo.GetPagedAsync(
                page, 20,
                filter: t => t.Estado != "Cerrado" && (t.TecnicoAsignadoId == null || t.TecnicoAsignadoId == userId),
                orderBy: q => q.OrderBy(t => t.FechaCreacion),   // Los más antiguos primero
                includeProperties: "CreadoPor,Equipo,TecnicoAsignado"
            );

            return View(tickets);
        }

        // POST: Tickets/TomarTicket/5
        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TomarTicket(int id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.TecnicoAsignadoId == null)
            {
                ticket.TecnicoAsignadoId = userId;
                ticket.Estado = "Asignado";
                ticket.FechaAsignacion = DateTime.Now;
                await _ticketRepo.UpdateAsync(ticket);
                TempData["Success"] = "Ticket asignado a usted correctamente.";
            }
            else if (ticket.TecnicoAsignadoId == userId)
            {
                // Ya está asignado a él, no hacemos nada
            }
            else
            {
                TempData["Error"] = "Este ticket ya está asignado a otro técnico.";
            }

            return RedirectToAction(nameof(PanelTecnico));
        }

        // GET: Tickets/Clasificar/5
        [Authorize(Roles = "Tecnico,Admin")]
        public async Task<IActionResult> Clasificar(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo"
            )).Items.FirstOrDefault();

            if (ticket == null) return NotFound();

            var viewModel = new TicketClasificarViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo,
                Tipo = ticket.Tipo ?? "",
                Prioridad = ticket.Prioridad ?? "",
                TiposList = ObtenerTipos(),
                PrioridadesList = ObtenerPrioridades()
            };
            return View(viewModel);
        }

        // POST: Tickets/Clasificar/5
        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clasificar(TicketClasificarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.TiposList = ObtenerTipos();
                model.PrioridadesList = ObtenerPrioridades();
                return View(model);
            }

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            ticket.Tipo = model.Tipo;
            ticket.Prioridad = model.Prioridad;

            // Si el ticket estaba pendiente y ya tiene técnico, actualizar estado a "Asignado"
            if (ticket.Estado == "Pendiente" && ticket.TecnicoAsignadoId != null)
                ticket.Estado = "Asignado";

            await _ticketRepo.UpdateAsync(ticket);
            TempData["Success"] = "Ticket clasificado correctamente.";
            return RedirectToAction(nameof(PanelTecnico));
        }

        // GET: Tickets/Cerrar/5
        [Authorize(Roles = "Tecnico,Admin")]
        public async Task<IActionResult> Cerrar(int id)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var viewModel = new TicketCierreViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo
            };
            return View(viewModel);
        }

        // POST: Tickets/Cerrar/5
        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(TicketCierreViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            ticket.Estado = "Cerrado";
            ticket.ObservacionesCierre = model.Observaciones;
            ticket.FechaCierre = DateTime.Now;
            await _ticketRepo.UpdateAsync(ticket);

            TempData["Success"] = "Ticket cerrado correctamente.";
            return RedirectToAction(nameof(PanelTecnico));
        }

        #endregion

        #region Acciones para Admin (gestión y asignación)

        // GET: Tickets/Index (listado completo con filtros)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int page = 1, string? estado = null)
        {
            var tickets = await _ticketRepo.GetPagedAsync(
                page, 15,
                filter: string.IsNullOrEmpty(estado) ? null : t => t.Estado == estado,
                orderBy: q => q.OrderByDescending(t => t.FechaCreacion),
                includeProperties: "CreadoPor,TecnicoAsignado,Equipo"
            );
            ViewBag.Estado = estado;
            return View(tickets);
        }

        // GET: Tickets/Asignar/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Asignar(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo"
            )).Items.FirstOrDefault();

            if (ticket == null) return NotFound();

            var viewModel = new TicketAsignarViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo,
                TecnicoAsignadoId = ticket.TecnicoAsignadoId,
                TecnicosList = await ObtenerTecnicos(ticket.TecnicoAsignadoId)
            };
            return View(viewModel);
        }

        // POST: Tickets/Asignar/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(TicketAsignarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.TecnicosList = await ObtenerTecnicos(model.TecnicoAsignadoId);
                return View(model);
            }

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            ticket.TecnicoAsignadoId = model.TecnicoAsignadoId;
            ticket.Estado = "Asignado";
            ticket.FechaAsignacion = DateTime.Now;
            await _ticketRepo.UpdateAsync(ticket);

            TempData["Success"] = "Ticket asignado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Métodos auxiliares

        private async Task<IEnumerable<SelectListItem>> ObtenerEquipos()
        {
            return (await _equipoRepo.GetPagedAsync(1, 100, filter: e => e.Activo, orderBy: q => q.OrderBy(e => e.Nombre)))
                .Items.Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.Nombre} ({e.Tipo})"
                });
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerTecnicos(int? seleccionado = null)
        {
            // Obtener usuarios con rol "Tecnico"
            var tecnicos = await _usuarioRepo.GetPagedAsync(1, 100,
                filter: u => u.Activo && u.Rol.Nombre == "Tecnico",
                includeProperties: "Rol",
                orderBy: q => q.OrderBy(u => u.Nombre));

            return tecnicos.Items.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.Nombre,
                Selected = (seleccionado.HasValue && u.Id == seleccionado.Value)
            });
        }

        private List<SelectListItem> ObtenerTipos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Hardware", Text = "Hardware" },
                new SelectListItem { Value = "Software", Text = "Software" },
                new SelectListItem { Value = "Red", Text = "Red" },
                new SelectListItem { Value = "Periférico", Text = "Periférico" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
        }

        private List<SelectListItem> ObtenerPrioridades()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Baja", Text = "Baja" },
                new SelectListItem { Value = "Media", Text = "Media" },
                new SelectListItem { Value = "Alta", Text = "Alta" },
                new SelectListItem { Value = "Urgente", Text = "Urgente" }
            };
        }

        #endregion
    }
}