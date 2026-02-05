using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Recepcion")]
    public class PacientesController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;

        public PacientesController(IGenericRepository<Paciente> pacientesRepo)
        {
            _pacientesRepo = pacientesRepo;
        }

        // LISTAR (Solo Admin y Recepcion)
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _pacientesRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: 10,
                orderBy: q => q.OrderByDescending(p => p.FechaRegistro)
            );

            return View(result);
        }

        // REGISTRO PÚBLICO (Sin autenticación)
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(PacienteViewModel model)
        {
            // Validación manual: Contraseña es obligatoria en el registro público
            if (string.IsNullOrWhiteSpace(model.Contrasena))
            {
                ModelState.AddModelError(nameof(model.Contrasena), "La contraseña es obligatoria.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar si ya existe un paciente con la misma cédula
            var pacientesExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == model.Cedula
            );

            if (pacientesExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Cedula), "Ya existe un paciente registrado con esta cédula.");
                return View(model);
            }

            // Verificar si ya existe un paciente con el mismo correo
            var emailsExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Email == model.Email
            );

            if (emailsExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Email), "Este correo electrónico ya está en uso. Por favor, use otro.");
                return View(model);
            }

            var entity = new Paciente
            {
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                Cedula = model.Cedula,
                Telefono = model.Telefono,
                Email = model.Email,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena!), // Hashear contraseña
                FechaNacimiento = model.FechaNacimiento,
                FechaRegistro = DateTime.Now
            };

            await _pacientesRepo.AddAsync(entity);

            TempData["Success"] = "Paciente registrado exitosamente. Ahora puede iniciar sesión.";
            return RedirectToAction("Login", "Account");
        }

        // CREATE (Solo Admin y Recepcion)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PacienteViewModel model)
        {
            // Validación manual: Contraseña es obligatoria al crear desde Admin/Recepcion
            if (string.IsNullOrWhiteSpace(model.Contrasena))
            {
                ModelState.AddModelError(nameof(model.Contrasena), "La contraseña es obligatoria.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar si ya existe un paciente con la misma cédula
            var pacientesExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == model.Cedula
            );

            if (pacientesExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Cedula), "Ya existe un paciente registrado con esta cédula.");
                return View(model);
            }

            // Verificar si ya existe un paciente con el mismo correo
            var emailsExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Email == model.Email
            );

            if (emailsExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Email), "Este correo electrónico ya está en uso. Por favor, use otro.");
                return View(model);
            }

            var entity = new Paciente
            {
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                Cedula = model.Cedula,
                Telefono = model.Telefono,
                Email = model.Email,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena!),
                FechaNacimiento = model.FechaNacimiento,
                FechaRegistro = DateTime.Now
            };

            await _pacientesRepo.AddAsync(entity);

            TempData["Success"] = "Paciente creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _pacientesRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            return View(new PacienteViewModel
            {
                Id = entity.Id,
                Nombres = entity.Nombres,
                Apellidos = entity.Apellidos,
                Cedula = entity.Cedula,
                Telefono = entity.Telefono,
                Email = entity.Email,
                FechaNacimiento = entity.FechaNacimiento
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PacienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = await _pacientesRepo.GetByIdAsync(model.Id);
            if (entity == null) return NotFound();

            // Verificar si la cédula ya existe en otro paciente
            var pacientesExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == model.Cedula && p.Id != model.Id
            );

            if (pacientesExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Cedula), "Ya existe otro paciente con esta cédula.");
                return View(model);
            }

            // Verificar si el correo ya existe en otro paciente
            var emailsExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Email == model.Email && p.Id != model.Id
            );

            if (emailsExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Email), "Este correo electrónico ya está en uso.");
                return View(model);
            }

            entity.Nombres = model.Nombres;
            entity.Apellidos = model.Apellidos;
            entity.Cedula = model.Cedula;
            entity.Telefono = model.Telefono;
            entity.Email = model.Email;
            
            // Solo actualizar contraseña si se proporcionó una nueva
            if (!string.IsNullOrWhiteSpace(model.Contrasena))
            {
                entity.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena);
            }
            
            entity.FechaNacimiento = model.FechaNacimiento;

            await _pacientesRepo.UpdateAsync(entity);

            TempData["Success"] = "Paciente actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }

    }
}
