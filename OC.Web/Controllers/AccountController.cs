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

        public AccountController(IGenericRepository<Usuario> userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Buscar usuario por correo (Incluimos el Rol para los Claims)
            var users = await _userRepository.GetPagedAsync(1, 1, u => u.Correo == model.Email, includeProperties: "Rol");
            var user = users.Items.FirstOrDefault();

            // 2. Verificar existencia y contraseña
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Contrasena) || !user.Activo)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas o cuenta desactivada.");
                return View(model);
            }

            // 3. Crear los "Claims" (Datos que viajan en la Cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Nombre),
                new Claim(ClaimTypes.Email, user.Correo),
                new Claim(ClaimTypes.Role, user.Rol.Nombre),
                new Claim("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 4. Iniciar sesión
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}