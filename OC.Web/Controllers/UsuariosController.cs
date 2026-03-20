using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace OC.Web.Controllers
{
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
            var viewModel = new UserViewModel
            {
                RolesList = await ObtenerRoles(),
                SucursalesList = await ObtenerSucursales()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            // Validación manual: Password es obligatorio al crear
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");
            }

            // NUEVO: Validar unicidad de cédula
            var existeCedula = (await _userRepository.GetPagedAsync(1, 1, u => u.Cedula == model.Cedula)).Items.Any();
            if (existeCedula)
            {
                ModelState.AddModelError(nameof(model.Cedula), "Ya existe un usuario con esa cédula.");
            }

            // Validar unicidad de correo (ya existente, pero lo dejamos igual)
            var existeCorreo = (await _userRepository.GetPagedAsync(1, 1, u => u.Correo == model.Email)).Items.Any();
            if (existeCorreo)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese correo.");
            }

            if (ModelState.IsValid)
            {
                var entity = new Usuario
                {
                    Nombre = model.Name,
                    Correo = model.Email,
                    Cedula = model.Cedula, // NUEVO
                    Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    RolId = model.RoleId,
                    SucursalId = model.SucursalId,
                    Activo = true
                };

                // NUEVO: Solo admin puede asignar campos salariales
                if (User.IsInRole("Admin"))
                {
                    entity.SalarioBase = model.SalarioBase;
                    entity.FechaContratacion = model.FechaContratacion;
                    entity.NumeroCuentaIBAN = model.NumeroCuentaIBAN;
                }

                await _userRepository.AddAsync(entity);
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model);
            return View(model);
        }

        // --- ASSIGN ROLE ---
        [HttpGet]
        public async Task<IActionResult> AssignRole()
        {
            var model = new RoleAssignmentViewModel();
            await LoadAssignmentDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(RoleAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetByIdAsync(model.UserId!.Value);
                if (user == null)
                {
                    ModelState.AddModelError(nameof(model.UserId), "El usuario seleccionado no existe.");
                }

                var role = await _roleRepository.GetByIdAsync(model.RoleId!.Value);
                if (role == null)
                {
                    ModelState.AddModelError(nameof(model.RoleId), "El rol seleccionado no existe.");
                }

                if (ModelState.IsValid)
                {
                    user!.RolId = role!.Id;
                    await _userRepository.UpdateAsync(user);

                    TempData["Success"] = "Rol asignado correctamente. Los permisos fueron actualizados.";
                    return RedirectToAction(nameof(AssignRole));
                }
            }

            await LoadAssignmentDropdowns(model);
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
                Cedula = entity.Cedula, // NUEVO
                RoleId = entity.RolId,
                SucursalId = entity.SucursalId,
                SalarioBase = entity.SalarioBase, // NUEVO
                FechaContratacion = entity.FechaContratacion, // NUEVO
                NumeroCuentaIBAN = entity.NumeroCuentaIBAN // NUEVO
            };

            await LoadDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel model)
        {
            // NUEVO: Validar unicidad de cédula (excluyendo el actual)
            var existeCedula = (await _userRepository.GetPagedAsync(1, 1, u => u.Cedula == model.Cedula && u.Id != model.Id)).Items.Any();
            if (existeCedula)
            {
                ModelState.AddModelError(nameof(model.Cedula), "Ya existe otro usuario con esa cédula.");
            }

            // Validar unicidad de correo (excluyendo el actual)
            var existeCorreo = (await _userRepository.GetPagedAsync(1, 1, u => u.Correo == model.Email && u.Id != model.Id)).Items.Any();
            if (existeCorreo)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe otro usuario con ese correo.");
            }

            if (ModelState.IsValid)
            {
                var entity = await _userRepository.GetByIdAsync(model.Id);
                if (entity == null) return NotFound();

                entity.Nombre = model.Name;
                entity.Correo = model.Email;
                entity.Cedula = model.Cedula; // NUEVO
                entity.RolId = model.RoleId;
                entity.SucursalId = model.SucursalId;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    entity.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                // NUEVO: Solo admin puede actualizar campos salariales
                if (User.IsInRole("Admin"))
                {
                    entity.SalarioBase = model.SalarioBase;
                    entity.FechaContratacion = model.FechaContratacion;
                    entity.NumeroCuentaIBAN = model.NumeroCuentaIBAN;
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

        private async Task LoadAssignmentDropdowns(RoleAssignmentViewModel model)
        {
            var roles = await _roleRepository.GetPagedAsync(1, 100);
            var users = await _userRepository.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                orderBy: q => q.OrderBy(u => u.Nombre)
            );

            model.RolesList = roles.Items.Select(x => new SelectListItem { Text = x.Nombre, Value = x.Id.ToString() });
            model.UsersList = users.Items.Select(x => new SelectListItem { Text = $"{x.Nombre} ({x.Correo})", Value = x.Id.ToString() });
        }

        // NUEVO: Métodos auxiliares para dropdowns (simplifican LoadDropdowns)
        private async Task<IEnumerable<SelectListItem>> ObtenerRoles(int? seleccionado = null)
        {
            var roles = await _roleRepository.GetPagedAsync(1, 100, orderBy: q => q.OrderBy(r => r.Nombre));
            return roles.Items.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nombre,
                Selected = (seleccionado.HasValue && r.Id == seleccionado.Value)
            });
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerSucursales(int? seleccionado = null)
        {
            var sucursales = await _branchRepository.GetPagedAsync(1, 100, filter: s => s.Activo, orderBy: q => q.OrderBy(s => s.Nombre));
            return sucursales.Items.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Nombre,
                Selected = (seleccionado.HasValue && s.Id == seleccionado.Value)
            });
        }
    }
}