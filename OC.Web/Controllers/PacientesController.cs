using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Helpers;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Recepcion,Optometrista")]
    public class PacientesController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;

        public PacientesController(IGenericRepository<Paciente> pacientesRepo)
        {
            _pacientesRepo = pacientesRepo;
        }

        // LISTAR con búsqueda por nombre o cédula
        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Búsqueda por nombre o cédula (insensible a mayúsculas)
                var results = await _pacientesRepo.GetPagedAsync(
                    pageIndex: page,
                    pageSize: 100, // Mostrar todos los resultados en una página
                    filter: p => p.Nombres.Contains(searchTerm) || p.Apellidos.Contains(searchTerm) || p.Cedula.Contains(searchTerm),
                    includeProperties: "Citas.Expediente" // Incluir citas y expedientes
                );
                ViewBag.SearchTerm = searchTerm;
                return View(results);
            }
            else
            {
                // Listado normal paginado
                var result = await _pacientesRepo.GetPagedAsync(
                    pageIndex: page,
                    pageSize: 10,
                    orderBy: q => q.OrderByDescending(p => p.FechaRegistro)
                );
                return View(result);
            }
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
            if (string.IsNullOrWhiteSpace(model.Contrasena))
                ModelState.AddModelError(nameof(model.Contrasena), "La contraseña es obligatoria.");

            var cedulaNormalizada = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNormalizada))
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View(model);
            }
            model.Cedula = cedulaNormalizada;

            if (!ModelState.IsValid)
                return View(model);

            var pacientesExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == model.Cedula
            );
            if (pacientesExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula ya existe. No puede continuar.");
                return View(model);
            }

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
                Cedula = cedulaNormalizada,
                Telefono = model.Telefono,
                Email = model.Email,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena!),
                FechaNacimiento = model.FechaNacimiento,
                FechaRegistro = DateTime.Now
            };

            await _pacientesRepo.AddAsync(entity);

            TempData["Success"] = "Paciente registrado exitosamente. Ahora puede iniciar sesión como paciente.";
            return RedirectToAction("Login", "PacienteAccount");
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
            if (string.IsNullOrWhiteSpace(model.Contrasena))
            {
                ModelState.AddModelError(nameof(model.Contrasena), "La contraseña es obligatoria.");
            }

            var cedulaNorm = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View(model);
            }
            model.Cedula = cedulaNorm;

            if (!ModelState.IsValid)
                return View(model);

            var pacientesExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == model.Cedula
            );
            if (pacientesExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula ya existe. No puede continuar.");
                return View(model);
            }

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
                Cedula = cedulaNorm,
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

            ViewBag.Bloqueado = entity.BloqueadoPermanentemente || (entity.BloqueadoHastaUtc.HasValue && entity.BloqueadoHastaUtc.Value > DateTime.UtcNow);

            return View(new PacienteViewModel
            {
                Id = entity.Id,
                Nombres = entity.Nombres,
                Apellidos = entity.Apellidos,
                Cedula = CedulaValidation.FormatearParaMostrar(entity.Cedula),
                Telefono = entity.Telefono,
                Email = entity.Email,
                FechaNacimiento = entity.FechaNacimiento
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PacienteViewModel model)
        {
            var cedulaNorm = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View(model);
            }
            model.Cedula = cedulaNorm;

            if (!ModelState.IsValid)
                return View(model);

            var entity = await _pacientesRepo.GetByIdAsync(model.Id);
            if (entity == null) return NotFound();

            var pacientesExistentes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == model.Cedula && p.Id != model.Id
            );
            if (pacientesExistentes.Items.Any())
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula ya existe. No puede continuar.");
                return View(model);
            }

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
            entity.Cedula = cedulaNorm;
            entity.Telefono = model.Telefono;
            entity.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.Contrasena))
            {
                entity.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena);
            }

            entity.FechaNacimiento = model.FechaNacimiento;

            await _pacientesRepo.UpdateAsync(entity);

            TempData["Success"] = "Paciente actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desbloquear(int id)
        {
            var entity = await _pacientesRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.IntentosFallidosLogin = 0;
            entity.BloqueadoHastaUtc = null;
            entity.BloqueadoPermanentemente = false;
            await _pacientesRepo.UpdateAsync(entity);

            TempData["Success"] = "Paciente desbloqueado correctamente.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBloqueo(int id)
        {
            var entity = await _pacientesRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            // Toggle: apagado = desbloqueado, encendido = bloqueado
            var ahoraBloqueado = !(entity.BloqueadoPermanentemente || (entity.BloqueadoHastaUtc.HasValue && entity.BloqueadoHastaUtc.Value > DateTime.UtcNow));
            if (ahoraBloqueado)
            {
                entity.BloqueadoPermanentemente = true;
                entity.BloqueadoHastaUtc = null;
            }
            else
            {
                entity.IntentosFallidosLogin = 0;
                entity.BloqueadoHastaUtc = null;
                entity.BloqueadoPermanentemente = false;
            }

            await _pacientesRepo.UpdateAsync(entity);
            TempData["Success"] = ahoraBloqueado ? "Paciente bloqueado." : "Paciente desbloqueado.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }
}