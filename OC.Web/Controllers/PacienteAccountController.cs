using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [AllowAnonymous]
    public class PacienteAccountController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;

        public PacienteAccountController(IGenericRepository<Paciente> pacientesRepo)
        {
            _pacientesRepo = pacientesRepo;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string cedula)
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

            // Crear los Claims para el paciente
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, paciente.NombreCompleto),
                new Claim(ClaimTypes.Role, "Paciente"),
                new Claim("PacienteId", paciente.Id.ToString()),
                new Claim("Cedula", paciente.Cedula)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Iniciar sesión
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = false });

            return RedirectToAction("Index", "PacienteDashboard");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
