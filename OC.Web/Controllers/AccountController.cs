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

                var nowUtc = DateTime.UtcNow;
                if (paciente?.BloqueadoPermanentemente == true)
                {
                    ViewBag.IsLockedPermanent = true;
                    ModelState.AddModelError(string.Empty, "Cuenta bloqueada. Contacte al administrador para desbloquearla.");
                    return View(model);
                }

                if (paciente?.BloqueadoHastaUtc.HasValue == true && paciente.BloqueadoHastaUtc.Value > nowUtc)
                {
                    var restante = paciente.BloqueadoHastaUtc.Value - nowUtc;
                    var segundos = Math.Max(0, (int)Math.Ceiling(restante.TotalSeconds));
                    ViewBag.LockoutSeconds = segundos;
                    ModelState.AddModelError(string.Empty, "Usuario bloqueado temporalmente. Espere a que finalice el tiempo de bloqueo.");
                    return View(model);
                }

                var credencialesOk = paciente != null
                    && !string.IsNullOrEmpty(paciente.Contrasena)
                    && BCrypt.Net.BCrypt.Verify(model.Password, paciente.Contrasena);

                if (!credencialesOk)
                {
                    if (paciente != null)
                    {
                        paciente.IntentosFallidosLogin++;

                        // A partir del 3er fallo se bloquea: 5,10,15,20,25 (máx). En el 5º fallo se bloquea permanentemente.
                        if (paciente.IntentosFallidosLogin >= 3)
                        {
                            var minutosBloqueo = Math.Min(25, (paciente.IntentosFallidosLogin - 2) * 5);
                            paciente.BloqueadoHastaUtc = nowUtc.AddMinutes(minutosBloqueo);

                            // Desde el 5º fallo: ya no permitir más intentos, requiere admin
                            if (paciente.IntentosFallidosLogin >= 5)
                            {
                                paciente.BloqueadoPermanentemente = true;
                                paciente.BloqueadoHastaUtc = null;
                            }

                            await _pacientesRepo.UpdateAsync(paciente);

                            if (paciente.BloqueadoPermanentemente)
                            {
                                ViewBag.IsLockedPermanent = true;
                                ModelState.AddModelError(string.Empty, "Cuenta bloqueada. Contacte al administrador para desbloquearla y asignar una nueva contraseña. Si necesita ayuda, llame al 2222-3333.");
                                return View(model);
                            }

                            ViewBag.LockoutSeconds = minutosBloqueo * 60;
                            var mensaje = $"Usuario bloqueado. Tiempo de espera: {minutosBloqueo} minuto(s).";
                            if (paciente.IntentosFallidosLogin >= 5)
                                mensaje += " Si necesita ayuda, llame al 2222-3333.";
                            ModelState.AddModelError(string.Empty, mensaje);
                            return View(model);
                        }

                        await _pacientesRepo.UpdateAsync(paciente);
                        ViewBag.AttemptsLeft = 3 - paciente.IntentosFallidosLogin;
                    }

                    ModelState.AddModelError(string.Empty,
                        ViewBag.AttemptsLeft != null
                            ? $"Credenciales incorrectas. Tiene {ViewBag.AttemptsLeft} intento(s) más."
                            : "Credenciales incorrectas.");
                    return View(model);
                }

                // Login exitoso como Paciente: resetear contadores/bloqueos
                if (paciente!.IntentosFallidosLogin != 0 || paciente.BloqueadoHastaUtc.HasValue || paciente.BloqueadoPermanentemente)
                {
                    paciente.IntentosFallidosLogin = 0;
                    paciente.BloqueadoHastaUtc = null;
                    paciente.BloqueadoPermanentemente = false;
                    await _pacientesRepo.UpdateAsync(paciente);

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