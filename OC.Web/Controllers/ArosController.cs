using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArosController : Controller
    {
        private readonly IGenericRepository<Aro> _aroRepo;
        private readonly IAroImagenRepository _imagenRepo;

        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExtensionesImagen = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long TamanoMaxImagenBytes = 5 * 1024 * 1024;
        private const int MaxImagenesPorAro = 7;

        public ArosController(
            IGenericRepository<Aro> aroRepo,
            IAroImagenRepository imagenRepo,
            IWebHostEnvironment env)
        {
            _aroRepo = aroRepo;
            _imagenRepo = imagenRepo;
            _env = env;
        }

        public IActionResult Create()
        {
            ViewBag.MaxImagenes = MaxImagenesPorAro;
            return View(new Aro());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                nameof(Aro.Nombre),
                nameof(Aro.SKU),
                nameof(Aro.Precio),
                nameof(Aro.Stock),
                nameof(Aro.DescripcionCorta),
                nameof(Aro.MostrarEnLanding))]
            Aro model,
            List<IFormFile>? imagenesAro)
        {
            if (imagenesAro == null || imagenesAro.Count == 0)
            {
                var archivos = Request.Form.Files.GetFiles("imagenesAro");
                if (archivos.Count > 0) imagenesAro = archivos.ToList();
            }

            if (string.IsNullOrWhiteSpace(model.SKU))
                ModelState.AddModelError(nameof(Aro.SKU), "El SKU es requerido.");

            var skuNorm = (model.SKU ?? "").Trim().ToUpperInvariant();
            var existente = await _aroRepo.GetPagedAsync(1, 1, a => a.SKU == skuNorm);
            if (existente.TotalCount > 0)
                ModelState.AddModelError(nameof(Aro.SKU), "Ya existe un aro con este SKU.");

            if (imagenesAro != null && imagenesAro.Count > 0)
            {
                if (imagenesAro.Count > MaxImagenesPorAro)
                {
                    ModelState.AddModelError("imagenesAro",
                        $"Máximo {MaxImagenesPorAro} imágenes por aro.");
                }
                else
                {
                    foreach (var img in imagenesAro.Where(i => i.Length > 0))
                    {
                        var err = ValidarImagen(img);
                        if (err != null)
                        {
                            ModelState.AddModelError("imagenesAro", err);
                            break;
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MaxImagenes = MaxImagenesPorAro;
                return View(model);
            }

            model.SKU = skuNorm;
            model.Activo = true;
            model.RutaImagen = null;

            await _aroRepo.AddAsync(model);

            if (imagenesAro != null && imagenesAro.Count > 0)
            {
                int orden = 1;
                bool primeraEsPrincipal = true;
                foreach (var img in imagenesAro.Where(i => i.Length > 0))
                {
                    var ruta = await GuardarImagenAroAsync(img);
                    var entidad = new AroImagen
                    {
                        AroId = model.Id,
                        Ruta = ruta,
                        Orden = orden++,
                        EsPrincipal = primeraEsPrincipal,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };
                    await _imagenRepo.AddAsync(entidad);
                    primeraEsPrincipal = false;
                }
            }

            TempData["Success"] = "Aro registrado correctamente.";
            return RedirectToAction("Index", "Inventory", new { seccion = "aros" });
        }

        public async Task<IActionResult> Details(int id)
        {
            var aro = await _aroRepo.GetByIdAsync(id);
            if (aro == null) return NotFound();

            // Galería para PhotoSwipe (todas las activas, ordenadas por principal+orden)
            var imagenes = await _imagenRepo.GetAllByAroIdAsync(id);
            var galeria = imagenes
                .Where(i => i.Activo)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Id)
                .ToList();

            ViewBag.Galeria = galeria;
            ViewBag.MaxImagenesPorAro = MaxImagenesPorAro;
            return View(aro);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var aro = await _aroRepo.GetByIdAsync(id);
            if (aro == null) return NotFound();

            var imagenes = await _imagenRepo.GetAllByAroIdAsync(id);
            ViewBag.Imagenes = imagenes;
            ViewBag.MaxImagenes = MaxImagenesPorAro;

            return View(aro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind(
                nameof(Aro.Id),
                nameof(Aro.Nombre),
                nameof(Aro.SKU),
                nameof(Aro.Precio),
                nameof(Aro.Stock),
                nameof(Aro.Activo),
                nameof(Aro.DescripcionCorta),
                nameof(Aro.MostrarEnLanding))]
            Aro model,
            List<IFormFile>? imagenesAro,
            int? imagenPrincipalId,
            string? ordenImagenes)
        {
            if (imagenesAro == null || imagenesAro.Count == 0)
            {
                var archivos = Request.Form.Files.GetFiles("imagenesAro");
                if (archivos.Count > 0) imagenesAro = archivos.ToList();
            }

            var existente = await _aroRepo.GetByIdAsync(model.Id);
            if (existente == null) return NotFound();

            var imagenesActuales = await _imagenRepo.GetActivasByAroIdAsync(model.Id);

            var idsAEliminar = new HashSet<int>();
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("eliminarImagen_")))
            {
                if (int.TryParse(Request.Form[key], out var idImg))
                    idsAEliminar.Add(idImg);
            }

            int totalTrasBorrado = imagenesActuales.Count - idsAEliminar.Count;
            if (imagenesAro != null && imagenesAro.Count > 0)
            {
                if (totalTrasBorrado + imagenesAro.Count > MaxImagenesPorAro)
                {
                    ModelState.AddModelError("imagenesAro",
                        $"Máximo {MaxImagenesPorAro} imágenes por aro. Tras borrar tiene {totalTrasBorrado} activas y quiere subir {imagenesAro.Count}.");
                }
                else
                {
                    foreach (var img in imagenesAro.Where(i => i.Length > 0))
                    {
                        var err = ValidarImagen(img);
                        if (err != null)
                        {
                            ModelState.AddModelError("imagenesAro", err);
                            break;
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                existente.Nombre = model.Nombre;
                existente.Precio = model.Precio;
                existente.Stock = model.Stock;
                existente.Activo = model.Activo;
                existente.MostrarEnLanding = model.MostrarEnLanding;

                var imagenesParaRepintar = await _imagenRepo.GetAllByAroIdAsync(model.Id);
                ViewBag.Imagenes = imagenesParaRepintar;
                ViewBag.MaxImagenes = MaxImagenesPorAro;
                return View(existente);
            }

            existente.Nombre = model.Nombre;
            existente.SKU = string.IsNullOrWhiteSpace(model.SKU)
                ? existente.SKU
                : model.SKU.Trim().ToUpperInvariant();
            existente.Precio = model.Precio;
            existente.Stock = model.Stock;
            existente.Activo = model.Activo;
            existente.MostrarEnLanding = model.MostrarEnLanding;
            existente.DescripcionCorta = model.DescripcionCorta;

            await _aroRepo.UpdateAsync(existente);

            foreach (var idImg in idsAEliminar)
            {
                await _imagenRepo.SoftDeleteAsync(idImg);
            }

            if (imagenesAro != null && imagenesAro.Count > 0)
            {
                int orden = imagenesActuales.Count - idsAEliminar.Count + 1;
                foreach (var img in imagenesAro.Where(i => i.Length > 0))
                {
                    var ruta = await GuardarImagenAroAsync(img);
                    var entidad = new AroImagen
                    {
                        AroId = existente.Id,
                        Ruta = ruta,
                        Orden = orden++,
                        EsPrincipal = false,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };
                    await _imagenRepo.AddAsync(entidad);
                }
            }

            if (imagenPrincipalId.HasValue && imagenPrincipalId.Value > 0)
            {
                var img = await _imagenRepo.GetByIdAsync(imagenPrincipalId.Value);
                if (img != null && img.AroId == existente.Id && img.Activo)
                {
                    await _imagenRepo.MarcarPrincipalAsync(imagenPrincipalId.Value);
                }
            }
            else
            {
                var principalActual = await _imagenRepo.GetPrincipalAsync(existente.Id);
                if (principalActual == null)
                {
                    var activas = await _imagenRepo.GetActivasByAroIdAsync(existente.Id);
                    if (activas.Count > 0)
                    {
                        await _imagenRepo.MarcarPrincipalAsync(activas[0].Id);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(ordenImagenes))
            {
                var pares = ordenImagenes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var par in pares)
                {
                    var partes = par.Split(':');
                    if (partes.Length == 2 &&
                        int.TryParse(partes[0], out var idImg) &&
                        int.TryParse(partes[1], out var ordenImg))
                    {
                        await _imagenRepo.UpdateOrdenAsync(idImg, ordenImg);
                    }
                }
            }

            TempData["Success"] = "Aro actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var aro = await _aroRepo.GetByIdAsync(id);
            if (aro == null) return NotFound();

            aro.Activo = false;
            await _aroRepo.UpdateAsync(aro);

            TempData["Success"] = "Aro desactivado.";
            return RedirectToAction("Index", "Inventory", new { seccion = "aros" });
        }

        // ============================================================
        // GESTIÓN DE IMÁGENES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GestionImagenes(int id)
        {
            var aro = await _aroRepo.GetByIdAsync(id);
            if (aro == null) return NotFound();

            var imagenes = await _imagenRepo.GetAllByAroIdAsync(id);
            ViewBag.Aro = aro;
            return View(imagenes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarImagenDefinitiva(int imagenId, int productoId)
        {
            var imagen = await _imagenRepo.GetByIdAsync(imagenId);
            if (imagen == null || imagen.AroId != productoId)
            {
                TempData["Error"] = "Imagen no encontrada.";
                return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
            }

            TryDeleteImagenFisica(imagen.Ruta);
            await _imagenRepo.DeleteAsync(imagenId);

            TempData["Success"] = "Imagen eliminada definitivamente.";
            return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurarImagen(int imagenId, int productoId)
        {
            var imagen = await _imagenRepo.GetByIdAsync(imagenId);
            if (imagen == null || imagen.AroId != productoId)
            {
                TempData["Error"] = "Imagen no encontrada.";
                return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
            }

            var activas = await _imagenRepo.CountActivasAsync(productoId);
            if (activas >= MaxImagenesPorAro)
            {
                TempData["Error"] = $"No se puede restaurar: ya hay {activas} imágenes activas (máximo {MaxImagenesPorAro}).";
                return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
            }

            await _imagenRepo.RestoreAsync(imagenId);

            TempData["Success"] = "Imagen restaurada.";
            return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
        }

        // ============================================================
        // HELPERS PRIVADOS
        // ============================================================

        private string? ValidarImagen(IFormFile file)
        {
            if (file.Length > TamanoMaxImagenBytes)
                return "La imagen no puede superar 5 MB.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesImagen.Contains(ext))
                return "Use JPG, PNG, GIF o WEBP.";
            return null;
        }

        private async Task<string> GuardarImagenAroAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "", "uploads", "aros");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesImagen.Contains(ext))
                ext = ".jpg";

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/aros/" + uniqueFileName;
        }

        private void TryDeleteImagenFisica(string? rutaRelativa)
        {
            if (string.IsNullOrEmpty(rutaRelativa) || _env.WebRootPath == null)
                return;
            if (!rutaRelativa.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return;

            var relative = rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(_env.WebRootPath, relative);
            if (System.IO.File.Exists(path))
            {
                try { System.IO.File.Delete(path); } catch { /* ignorar bloqueos */ }
            }
        }
    }
}
