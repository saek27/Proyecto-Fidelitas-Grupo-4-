using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OC.Core.Common;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Web.Helpers;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Optometrista,Recepcion,TecnicoOcular,Tecnico")]
    public class VacacionesController : Controller
    {
        private readonly AppDbContext _context;

        public VacacionesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int? usuarioId = null)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var esAdmin = User.IsInRole("Admin");

            var usuarioActual = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuarioActual == null)
                return RedirectToAction("Login", "Account");

            var saldoPropio = await ConstruirSaldoAsync(usuarioActual);

            VacacionesSaldoViewModel? saldoConsultado = null;
            IQueryable<SolicitudVacacion> query = _context.SolicitudesVacacion
                .Include(s => s.Usuario)
                .Include(s => s.AprobadoPor);

            if (esAdmin)
            {
                if (usuarioId.HasValue && usuarioId.Value > 0)
                {
                    var consultado = await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
                    if (consultado != null)
                    {
                        saldoConsultado = await ConstruirSaldoAsync(consultado);
                        query = query.Where(s => s.UsuarioId == usuarioId.Value);
                    }
                }
            }
            else
            {
                query = query.Where(s => s.UsuarioId == userId);
            }

            const int pageSize = 10;
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.FechaSolicitud)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new VacacionesIndexViewModel
            {
                EsAdmin = esAdmin,
                Saldo = saldoPropio,
                SaldoConsultado = saldoConsultado,
                UsuarioConsultaId = usuarioId,
                Solicitudes = new PagedResult<SolicitudVacacion>(items, totalItems, page, pageSize),
                UsuariosFiltro = esAdmin ? await ObtenerTrabajadoresAsync() : []
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario == null)
                return RedirectToAction("Login", "Account");

            var saldo = await ConstruirSaldoAsync(usuario);
            if (!saldo.PuedeSolicitar)
            {
                TempData["Error"] = saldo.MotivoNoSolicitar ?? "No puede solicitar vacaciones en este momento.";
                return RedirectToAction(nameof(Index));
            }

            return View(new SolicitudVacacionCreateViewModel { Saldo = saldo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudVacacionCreateViewModel model)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario == null)
                return RedirectToAction("Login", "Account");

            var solicitudes = await _context.SolicitudesVacacion
                .Where(s => s.UsuarioId == userId)
                .ToListAsync();

            var saldo = ConstruirSaldoDesdeDatos(usuario, solicitudes);
            model.Saldo = saldo;

            if (model.FechaFin.Date < model.FechaInicio.Date)
                ModelState.AddModelError(nameof(model.FechaFin), "La fecha fin no puede ser anterior a la fecha inicio.");

            var dias = VacacionesHelper.ContarDiasCalendario(model.FechaInicio, model.FechaFin);
            if (dias <= 0)
                ModelState.AddModelError(nameof(model.FechaFin), "El rango de fechas no es válido.");

            if (!saldo.TieneFechaContratacion)
                ModelState.AddModelError(string.Empty, "No tiene fecha de contratación registrada. Solicite al administrador que la complete en Usuarios.");

            if (saldo.TieneSolicitudPendiente)
                ModelState.AddModelError(string.Empty, "Ya tiene una solicitud pendiente. Espere la respuesta del administrador.");

            if (dias > saldo.DiasDisponibles)
                ModelState.AddModelError(string.Empty, $"Solo tiene {saldo.DiasDisponibles} día(s) disponible(s). Está solicitando {dias}.");

            if (!ModelState.IsValid)
                return View(model);

            var solicitud = new SolicitudVacacion
            {
                UsuarioId = userId,
                FechaInicio = model.FechaInicio.Date,
                FechaFin = model.FechaFin.Date,
                DiasSolicitados = dias,
                Motivo = model.Motivo?.Trim(),
                Estado = VacacionesHelper.EstadoPendiente,
                FechaSolicitud = DateTime.Now
            };

            _context.SolicitudesVacacion.Add(solicitud);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud de vacaciones enviada. El administrador la revisará pronto.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var adminId = int.Parse(User.FindFirst("UserId")!.Value);
            var solicitud = await _context.SolicitudesVacacion
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != VacacionesHelper.EstadoPendiente)
            {
                TempData["Error"] = "Esta solicitud ya fue procesada.";
                return RedirectToAction(nameof(Index));
            }

            var solicitudesUsuario = await _context.SolicitudesVacacion
                .Where(s => s.UsuarioId == solicitud.UsuarioId)
                .ToListAsync();

            var saldo = ConstruirSaldoDesdeDatos(solicitud.Usuario!, solicitudesUsuario);
            if (solicitud.DiasSolicitados > saldo.DiasDisponibles)
            {
                TempData["Error"] = $"No se puede aprobar: el usuario solo tiene {saldo.DiasDisponibles} día(s) disponible(s).";
                return RedirectToAction(nameof(Index), new { usuarioId = solicitud.UsuarioId });
            }

            solicitud.Estado = VacacionesHelper.EstadoAprobado;
            solicitud.AprobadoPorId = adminId;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud de vacaciones aprobada.";
            return RedirectToAction(nameof(Index), new { usuarioId = solicitud.UsuarioId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var adminId = int.Parse(User.FindFirst("UserId")!.Value);
            var solicitud = await _context.SolicitudesVacacion.FindAsync(id);

            if (solicitud == null)
                return NotFound();

            if (solicitud.Estado != VacacionesHelper.EstadoPendiente)
            {
                TempData["Error"] = "Esta solicitud ya fue procesada.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = VacacionesHelper.EstadoRechazado;
            solicitud.AprobadoPorId = adminId;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud de vacaciones rechazada.";
            return RedirectToAction(nameof(Index), new { usuarioId = solicitud.UsuarioId });
        }

        private async Task<List<Usuario>> ObtenerTrabajadoresAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Rol)
                .Where(u => u.Activo && u.Rol.Nombre != "Paciente")
                .OrderBy(u => u.Nombre)
                .ToListAsync();
        }

        private async Task<VacacionesSaldoViewModel> ConstruirSaldoAsync(Usuario usuario)
        {
            var solicitudes = await _context.SolicitudesVacacion
                .AsNoTracking()
                .Where(s => s.UsuarioId == usuario.Id)
                .ToListAsync();

            return ConstruirSaldoDesdeDatos(usuario, solicitudes);
        }

        private static VacacionesSaldoViewModel ConstruirSaldoDesdeDatos(Usuario usuario, List<SolicitudVacacion> solicitudes)
        {
            var meses = usuario.FechaContratacion.HasValue
                ? VacacionesHelper.ContarMesesCompletos(usuario.FechaContratacion.Value)
                : 0;
            var acumulados = VacacionesHelper.CalcularDiasAcumulados(usuario.FechaContratacion);
            var usados = VacacionesHelper.CalcularDiasUsados(solicitudes);
            var disponibles = VacacionesHelper.CalcularDiasDisponibles(usuario.FechaContratacion, solicitudes);
            var pendiente = VacacionesHelper.TieneSolicitudPendiente(solicitudes);

            var saldo = new VacacionesSaldoViewModel
            {
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.Nombre,
                FechaContratacion = usuario.FechaContratacion,
                MesesCompletos = meses,
                DiasAcumulados = acumulados,
                DiasUsados = usados,
                DiasDisponibles = disponibles,
                TieneSolicitudPendiente = pendiente
            };

            if (!saldo.TieneFechaContratacion)
            {
                saldo.PuedeSolicitar = false;
                saldo.MotivoNoSolicitar = "No tiene fecha de contratación registrada. Solicite al administrador que la complete en Usuarios.";
            }
            else if (pendiente)
            {
                saldo.PuedeSolicitar = false;
                saldo.MotivoNoSolicitar = "Ya tiene una solicitud pendiente de aprobación.";
            }
            else if (disponibles <= 0)
            {
                saldo.PuedeSolicitar = false;
                saldo.MotivoNoSolicitar = "No tiene días de vacaciones disponibles.";
            }
            else
            {
                saldo.PuedeSolicitar = true;
            }

            return saldo;
        }
    }
}
