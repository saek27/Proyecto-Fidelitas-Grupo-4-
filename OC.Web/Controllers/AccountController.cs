using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IGenericRepository<Usuario> _userRepository;
        private readonly IGenericRepository<Paciente> _pacientesRepo;

        public AccountController(
            IGenericRepository<Usuario> userRepository,
            IGenericRepository<Paciente> pacientesRepo)
        {
            _userRepository = userRepository;
            _pacientesRepo = pacientesRepo;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Primero intentar buscar como Usuario del sistema (Admin/Recepcion/Optometrista)
            var users = await _userRepository.GetPagedAsync(1, 1, u => u.Correo == model.Email, includeProperties: "Rol");
            var user = users.Items.FirstOrDefault();

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Contrasena) && user.Activo)
            {
                // Login exitoso como Usuario del sistema
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Nombre),
                    new Claim(ClaimTypes.Email, user.Correo),
                    new Claim(ClaimTypes.Role, user.Rol.Nombre),
                    new Claim("UserId", user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = model.RememberMe });

                return RedirectToAction("Index", "Home");
            }

            // 2. Si no es usuario del sistema, intentar como Paciente
            try
            {
                var pacientes = await _pacientesRepo.GetPagedAsync(1, 1, p => p.Email == model.Email);
                var paciente = pacientes.Items.FirstOrDefault();

                if (paciente != null && !string.IsNullOrEmpty(paciente.Contrasena) && BCrypt.Net.BCrypt.Verify(model.Password, paciente.Contrasena))
                {
                    // Login exitoso como Paciente
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, paciente.NombreCompleto),
                        new Claim(ClaimTypes.Email, paciente.Email ?? ""),
                        new Claim(ClaimTypes.Role, "Paciente"),
                        new Claim("PacienteId", paciente.Id.ToString()),
                        new Claim("Cedula", paciente.Cedula)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties { IsPersistent = model.RememberMe });

                    return RedirectToAction("Index", "PacienteDashboard");
                }
            }
            catch
            {
                // Si la tabla no existe aún (migración no ejecutada), continuar
            }

            // 3. Si llegamos aquí, las credenciales son inválidas
            ModelState.AddModelError(string.Empty, "Credenciales inválidas o cuenta desactivada.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}