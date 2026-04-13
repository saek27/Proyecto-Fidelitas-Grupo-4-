using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExtensionesImagen = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long TamanoMaxImagenBytes = 5 * 1024 * 1024;

        public InventoryController(IGenericRepository<Producto> productoRepo, IWebHostEnvironment env)
        {
            _productoRepo = productoRepo;
            _env = env;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var result = await _productoRepo.GetPagedAsync(
                page,
                pageSize,
                p => p.Activo,
                q => q.OrderBy(p => p.Nombre)
            );

            var lowStock = await _productoRepo.GetAllAsync(
                p => p.Activo && p.Stock < 6
            );

            ViewBag.PageSize = pageSize;
            ViewBag.LowStock = lowStock;

            return View(result);
        }

        public IActionResult Create()
        {
            return View(new Producto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                nameof(Producto.Nombre),
                nameof(Producto.SKU),
                nameof(Producto.Categoria),
                nameof(Producto.PrecioPublico),
                nameof(Producto.DescripcionCorta),
                nameof(Producto.CostoUnitario),
                nameof(Producto.Stock),
                nameof(Producto.Destacado))]
            Producto model,
            IFormFile? imagenProducto)
        {
            imagenProducto ??= Request.Form.Files.GetFile("imagenProducto");

            if (string.IsNullOrWhiteSpace(model.SKU))
            {
                ModelState.AddModelError(nameof(Producto.SKU), "El SKU es requerido.");
            }

            var skuNorm = (model.SKU ?? "").Trim().ToUpperInvariant();
            var existente = await _productoRepo.GetPagedAsync(1, 1, p => p.SKU == skuNorm);
            if (existente.TotalCount > 0)
            {
                ModelState.AddModelError(nameof(Producto.SKU), "Ya existe un producto con este SKU.");
            }

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                var err = ValidarImagen(imagenProducto);
                if (err != null)
                    ModelState.AddModelError(nameof(imagenProducto), err);
            }

            if (!ModelState.IsValid)
                return View(model);

            model.SKU = skuNorm;
            model.Activo = true;
            model.RutaImagen = null;

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                model.RutaImagen = await GuardarImagenProductoAsync(imagenProducto);
            }

            await _productoRepo.AddAsync(model);

            TempData["Success"] = "Producto registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind(
                nameof(Producto.Id),
                nameof(Producto.Nombre),
                nameof(Producto.SKU),
                nameof(Producto.Categoria),
                nameof(Producto.PrecioPublico),
                nameof(Producto.DescripcionCorta),
                nameof(Producto.CostoUnitario),
                nameof(Producto.Stock),
                nameof(Producto.Destacado),
                nameof(Producto.Activo))]
            Producto model,
            IFormFile? imagenProducto)
        {
            imagenProducto ??= Request.Form.Files.GetFile("imagenProducto");

            var existente = await _productoRepo.GetByIdAsync(model.Id);
            if (existente == null)
                return NotFound();

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                var err = ValidarImagen(imagenProducto);
                if (err != null)
                    ModelState.AddModelError(nameof(imagenProducto), err);
            }

            if (!ModelState.IsValid)
            {
                existente.Nombre = model.Nombre;
                existente.Categoria = model.Categoria;
                existente.PrecioPublico = model.PrecioPublico;
                existente.DescripcionCorta = model.DescripcionCorta;
                existente.CostoUnitario = model.CostoUnitario;
                existente.Stock = model.Stock;
                existente.Destacado = model.Destacado;
                existente.Activo = model.Activo;
                return View(existente);
            }

            existente.Nombre = model.Nombre;
            existente.SKU = string.IsNullOrWhiteSpace(model.SKU)
                ? existente.SKU
                : model.SKU.Trim().ToUpperInvariant();
            existente.Categoria = model.Categoria;
            existente.PrecioPublico = model.PrecioPublico;
            existente.DescripcionCorta = model.DescripcionCorta;
            existente.CostoUnitario = model.CostoUnitario;
            existente.Stock = model.Stock;
            existente.Destacado = model.Destacado;
            existente.Activo = model.Activo;

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                TryDeleteImagenFisica(existente.RutaImagen);
                existente.RutaImagen = await GuardarImagenProductoAsync(imagenProducto);
            }

            await _productoRepo.UpdateAsync(existente);

            TempData["Success"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();

            producto.Activo = false;
            await _productoRepo.UpdateAsync(producto);

            TempData["Success"] = "Producto eliminado del catálogo.";
            return RedirectToAction(nameof(Index));
        }

        private string? ValidarImagen(IFormFile file)
        {
            if (file.Length > TamanoMaxImagenBytes)
                return "La imagen no puede superar 5 MB.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesImagen.Contains(ext))
                return "Use JPG, PNG, GIF o WEBP.";
            return null;
        }

        private async Task<string> GuardarImagenProductoAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "", "uploads", "productos");
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

            return "/uploads/productos/" + uniqueFileName;
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
