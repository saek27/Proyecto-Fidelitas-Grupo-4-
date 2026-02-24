using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Helpers;
using OC.Web.ViewModels;
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
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string cedula, string contrasena)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener exactamente 9 dígitos. Ejemplo: 604240201");
                return View();
            }
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                ModelState.AddModelError("", "Debe ingresar su contraseña.");
                return View();
            }

            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm
            );
            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null || string.IsNullOrEmpty(paciente.Contrasena) || !BCrypt.Net.BCrypt.Verify(contrasena, paciente.Contrasena))
            {
                ModelState.AddModelError("", "Cédula o contraseña incorrectos. Si olvidó su contraseña, use \"Recuperar contraseña\".");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, paciente.NombreCompleto),
                new Claim(ClaimTypes.Role, "Paciente"),
                new Claim("PacienteId", paciente.Id.ToString()),
                new Claim("Cedula", paciente.Cedula)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = false });

            return RedirectToAction("Index", "PacienteDashboard");
        }

        [HttpGet]
        public IActionResult RecuperarContrasena() => View();

        // Recuperación por cédula: token con expiración 1h
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(string cedula)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener exactamente 9 dígitos. Ejemplo: 604240201");
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
                TempData["Info"] = "Si la cédula está registrada, recibirá instrucciones para restablecer su contraseña.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            var token = Guid.NewGuid().ToString("N");
            paciente.TokenRecuperacion = token;
            paciente.FechaExpiracionToken = DateTime.UtcNow.AddHours(1);
            await _pacientesRepo.UpdateAsync(paciente);

            var link = Url.Action(nameof(RestablecerContrasena), "PacienteAccount", new { token }, Request.Scheme)!;
            TempData["TokenRecuperacion"] = link;
            TempData["Success"] = "Siga el enlace que se muestra a continuación para restablecer su contraseña. El enlace es válido por 1 hora y no debe compartirse.";
            return RedirectToAction(nameof(InstruccionesRecuperacion));
        }

        [HttpGet]
        public IActionResult InstruccionesRecuperacion()
        {
            var link = TempData["TokenRecuperacion"] as string;
            if (string.IsNullOrEmpty(link))
                return RedirectToAction(nameof(Login));
            ViewBag.LinkRecuperacion = link;
            return View("InstruccionesRecuperacion");
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));
            var paciente = await BuscarPacientePorTokenAsync(token);
            if (paciente == null)
            {
                TempData["Error"] = "El enlace ha expirado o no es válido. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }
            ViewBag.Token = token;
            return View(new RestablecerContrasenaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(string? token, RestablecerContrasenaViewModel model)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));
            ViewBag.Token = token;

            var paciente = await BuscarPacientePorTokenAsync(token);
            if (paciente == null)
            {
                TempData["Error"] = "El enlace ha expirado o no es válido. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            if (!ModelState.IsValid)
                return View(model);

            paciente.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.NuevaContrasena);
            paciente.TokenRecuperacion = null;
            paciente.FechaExpiracionToken = null;
            await _pacientesRepo.UpdateAsync(paciente);

            TempData["Success"] = "Su contraseña se ha restablecido correctamente. Ya puede iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        private async Task<Paciente?> BuscarPacientePorTokenAsync(string token)
        {
            var result = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.TokenRecuperacion == token && p.FechaExpiracionToken.HasValue && p.FechaExpiracionToken.Value > DateTime.UtcNow
            );
            return result.Items.FirstOrDefault();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
