using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    public class AsistenciaController : Controller
    {
        private readonly AppDbContext _context;

        public AsistenciaController(AppDbContext context)
        {
            _context = context;
        }

        // 📌 Marcar entrada
        public async Task<IActionResult> MarcarEntrada(int usuarioId)
        {
            var hoy = DateTime.Today;

            var asistencia = await _context.Asistencias
            .Where(a => a.UsuarioId == usuarioId && a.Fecha == hoy)
            .FirstOrDefaultAsync();

            if (asistencia != null)
            {
                return BadRequest("Ya marcó entrada hoy.");
            }

            var nueva = new Asistencia
            {
                UsuarioId = usuarioId,
                Fecha = hoy,
                HoraEntrada = DateTime.Now
            };

            _context.Asistencias.Add(nueva);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // 📌 Marcar salida
        public async Task<IActionResult> MarcarSalida(int usuarioId)
        {
            var hoy = DateTime.Today;

            var asistencia = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId && a.Fecha == hoy);

            if (asistencia == null || asistencia.HoraEntrada == null)
            {
                return BadRequest("No ha marcado entrada.");
            }

            if (asistencia.HoraSalida != null)
            {
                return BadRequest("Ya marcó salida.");
            }

            asistencia.HoraSalida = DateTime.Now;

            _context.Asistencias.Update(asistencia);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}