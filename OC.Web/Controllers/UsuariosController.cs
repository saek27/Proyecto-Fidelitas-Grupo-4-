using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace OC.Web.Controllers
{
    // Solo usuarios con el Claim de Rol "Admin" pueden entrar
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly IGenericRepository<Usuario> _userRepository;
        private readonly IGenericRepository<Rol> _roleRepository;
        private readonly IGenericRepository<Sucursal> _branchRepository;

        public UsuariosController(
            IGenericRepository<Usuario> userRepository,
            IGenericRepository<Rol> roleRepository,
            IGenericRepository<Sucursal> branchRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _branchRepository = branchRepository;
        }

        // --- READ ---
        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            var pagedResult = await _userRepository.GetPagedAsync(
                pageIndex: pageIndex,
                pageSize: 10,
                orderBy: q => q.OrderBy(u => u.Nombre),
                includeProperties: "Rol,Sucursal"
            );
            return View(pagedResult);
        }

        // --- CREATE ---
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserViewModel();
            await LoadDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            // Validación manual: Password es obligatorio al crear
            if (ModelState.IsValid)
            {
                var entity = new Usuario
                {
                    Nombre = model.Name,
                    Correo = model.Email,
                    // AQUÍ: Encriptamos antes de guardar
                    Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    RolId = model.RoleId,
                    SucursalId = model.SucursalId,
                    Activo = true
                };

                await _userRepository.AddAsync(entity);
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model);
            return View(model);
        }

        // --- EDIT ---
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _userRepository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var model = new UserViewModel
            {
                Id = entity.Id,
                Name = entity.Nombre,
                Email = entity.Correo,
                RoleId = entity.RolId,
                SucursalId = entity.SucursalId,
                
            };

            await LoadDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var entity = await _userRepository.GetByIdAsync(model.Id);
                if (entity == null) return NotFound();

                // Actualizamos campos
                entity.Nombre = model.Name;
                entity.Correo = model.Email;
                entity.RolId = model.RoleId;
                entity.SucursalId = model.SucursalId;

                // Solo cambiamos password si el usuario escribió algo nuevo
                if (!string.IsNullOrEmpty(model.Password))
                {
                    entity.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                await _userRepository.UpdateAsync(entity);
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model);
            return View(model);
        }

        // --- DELETE / TOGGLE STATUS (Soft Delete) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var entity = await _userRepository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            // Invertimos el estado (Si es true pasa a false, y viceversa)
            entity.Activo = !entity.Activo;

            await _userRepository.UpdateAsync(entity);

            return RedirectToAction(nameof(Index));
        }

        // --- HELPERS ---
        private async Task LoadDropdowns(UserViewModel model)
        {
            var roles = await _roleRepository.GetPagedAsync(1, 100);
            var branches = await _branchRepository.GetPagedAsync(1, 100);

            model.RolesList = roles.Items.Select(x => new SelectListItem { Text = x.Nombre, Value = x.Id.ToString() });
            model.SucursalesList = branches.Items.Select(x => new SelectListItem { Text = x.Nombre, Value = x.Id.ToString() });
        }
    }
}