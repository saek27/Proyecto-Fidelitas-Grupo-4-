using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OC.Core.Domain.Entities; using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OC.Core.Contracts.IRepositories;
using OC.Data.Context;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Optometrista,Recepcion")]
    public class PermisoController : Controller
    {
        private readonly AppDbContext _context;

        public PermisoController(AppDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Permiso> query = _context.Permisos
                .Include(p => p.Usuario)
                .Include(p => p.AprobadoPor);


            if (role != "Admin")
            {
                query = query.Where(p => p.UsuarioId == userId);
            }

            var data = await query
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();

            return View(data);
        }


        public async Task<IActionResult> Create()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;


            if (role == "Admin")
            {
                ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Permiso permiso)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 👤 Si NO es admin, forzar su propio usuario
            if (role != "Admin")
            {
                permiso.UsuarioId = userId;
            }

            permiso.Estado = "Pendiente";
            permiso.FechaSolicitud = DateTime.Now;

            _context.Permisos.Add(permiso);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var permiso = await _context.Permisos.FindAsync(id);

            if (permiso == null)
                return NotFound();

            permiso.Estado = "Aprobado";
            permiso.AprobadoPorId = userId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var permiso = await _context.Permisos.FindAsync(id);

            if (permiso == null)
                return NotFound();

            permiso.Estado = "Rechazado";
            permiso.AprobadoPorId = userId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}