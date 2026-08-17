using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Web.Models;
using System.Linq.Expressions;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<TecnologiaLente> _tecnologiaRepo;
        private readonly IGenericRepository<Aro> _aroRepo;
        private readonly IProductoImagenRepository _imagenRepo;
        private readonly ILandingCarruselRepository _landingCarruselRepo;
        private readonly ILandingDestacadoRepository _landingDestacadoRepo;
        private readonly AppDbContext _db;

        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExtensionesImagen = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long TamanoMaxImagenBytes = 5 * 1024 * 1024;
        private const int MaxImagenesPorProducto = 7;

        public InventoryController(
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<TecnologiaLente> tecnologiaRepo,
            IGenericRepository<Aro> aroRepo,
            IProductoImagenRepository imagenRepo,
            ILandingCarruselRepository landingCarruselRepo,
            ILandingDestacadoRepository landingDestacadoRepo,
            AppDbContext db,
            IWebHostEnvironment env)
        {
            _productoRepo = productoRepo;
            _tecnologiaRepo = tecnologiaRepo;
            _aroRepo = aroRepo;
            _imagenRepo = imagenRepo;
            _landingCarruselRepo = landingCarruselRepo;
            _landingDestacadoRepo = landingDestacadoRepo;
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index(
            string seccion = "productos",
            int page = 1, int pageSize = 12,
            string? filtroProducto = null,
            string? filtroTecnologia = null,
            int tecPage = 1, int tecPageSize = 12,
            string? filtroAro = null,
            int aroPage = 1, int aroPageSize = 12,
            int carruselDispPage = 1, int carruselDispPageSize = 12,
            int carruselSelPage = 1, int carruselSelPageSize = 12,
            int destacadosDispPage = 1, int destacadosDispPageSize = 12,
            int destacadosSelPage = 1, int destacadosSelPageSize = 12)
        {
            // Productos
            // Productos con filtro
            Expression<Func<Producto, bool>> prodFilter = p => p.Activo;
            if (!string.IsNullOrWhiteSpace(filtroProducto))
            {
                var lower = filtroProducto.ToLower();
                prodFilter = p => p.Activo && (p.Nombre.ToLower().Contains(lower) ||
                                                p.SKU.ToLower().Contains(lower));
            }
            var productos = await _productoRepo.GetPagedAsync(page, pageSize, prodFilter, q => q.OrderBy(p => p.Nombre));
            var lowStock = await _productoRepo.GetAllAsync(p => p.Activo && p.Stock < 6);

            // Tecnologias con filtro
            Expression<Func<TecnologiaLente, bool>> tecFilter = t => true;
            if (!string.IsNullOrWhiteSpace(filtroTecnologia))
            {
                var lower = filtroTecnologia.ToLower();
                tecFilter = t => t.Nombre.ToLower().Contains(lower) ||
                                 t.Precio.ToString().Contains(lower);
            }
            var tecnologias = await _tecnologiaRepo.GetPagedAsync(tecPage, tecPageSize, tecFilter, q => q.OrderBy(t => t.Nombre));

            // Aros con filtro
            Expression<Func<Aro, bool>> aroFilter = a => a.Activo;
            if (!string.IsNullOrWhiteSpace(filtroAro))
            {
                var lower = filtroAro.ToLower();
                aroFilter = a => a.Activo && (a.Nombre.ToLower().Contains(lower) ||
                                               a.SKU.ToLower().Contains(lower) ||
                                               a.Precio.ToString().Contains(lower) ||
                                               a.Stock.ToString().Contains(lower));
            }
            var aros = await _aroRepo.GetPagedAsync(aroPage, aroPageSize, aroFilter, q => q.OrderBy(a => a.Nombre));

            ViewBag.Seccion = seccion;
            ViewBag.PageSize = pageSize;
            ViewBag.LowStock = lowStock;
            ViewBag.FiltroProducto = filtroProducto;
            ViewBag.FiltroTecnologia = filtroTecnologia;
            ViewBag.FiltroAro = filtroAro;

            ViewBag.PaginationProductos = new PaginationInfo
            {
                CurrentPage = productos.PageIndex,
                TotalPages = productos.TotalPages,
                GetPageUrl = p => Url.Action("Index", new { seccion, page = p, pageSize, filtroProducto, filtroTecnologia, tecPage, tecPageSize, filtroAro, aroPage, aroPageSize })
            };
            ViewBag.PaginationTecnologias = new PaginationInfo
            {
                CurrentPage = tecnologias.PageIndex,
                TotalPages = tecnologias.TotalPages,
                GetPageUrl = p => Url.Action("Index", new { seccion, page, pageSize, filtroProducto, filtroTecnologia = filtroTecnologia, tecPage = p, tecPageSize, filtroAro, aroPage, aroPageSize })
            };
            ViewBag.PaginationAros = new PaginationInfo
            {
                CurrentPage = aros.PageIndex,
                TotalPages = aros.TotalPages,
                GetPageUrl = p => Url.Action("Index", new { seccion, page, pageSize, filtroProducto, filtroTecnologia, tecPage, tecPageSize, filtroAro = filtroAro, aroPage = p, aroPageSize })
            };
            ViewBag.Tecnologias = tecnologias.Items.ToList();
            ViewBag.Aros = aros.Items.ToList();

            // ── LANDING: 4 listas paginadas (carrusel disponibles/seleccionados, destacados disponibles/seleccionados) ──
            if (seccion == "landing")
            {
                var carrusel = await _landingCarruselRepo.GetAllAsync();
                var destacados = await _landingDestacadoRepo.GetAllAsync();

                // IDs ya asignados a slots
                var idsEnCarrusel = new HashSet<int>(
                    carrusel.Select(c => c.ProductoId ?? c.AroId ?? 0)
                        .Concat(destacados.Select(d => d.ProductoId ?? d.AroId ?? 0)));
                var idsEnCarruselProducto = new HashSet<int>(carrusel.Where(c => c.ProductoId.HasValue).Select(c => c.ProductoId!.Value));
                var idsEnCarruselAro = new HashSet<int>(carrusel.Where(c => c.AroId.HasValue).Select(c => c.AroId!.Value));
                var idsEnDestProducto = new HashSet<int>(destacados.Where(d => d.ProductoId.HasValue).Select(d => d.ProductoId!.Value));
                var idsEnDestAro = new HashSet<int>(destacados.Where(d => d.AroId.HasValue).Select(d => d.AroId!.Value));

                // Pool de productos y aros marcados (Activo=true, Destacado=true / MostrarEnLanding=true)
                // Se aplica ANTES de paginar para tener el total real.
                // Pool: todos los items MARCADOS (independiente de Activo).
                // Razón: el admin debe poder gestionar el landing incluso si el item está inactivo
                // (lo reactivará desde la pestaña Productos/Aros). El landing público filtra Activo por su cuenta.
                var todosProductosMarcados = (await _productoRepo.GetAllAsync(p => p.Destacado))
                    .OrderBy(p => p.Nombre).ToList();
                var todosArosMarcados = (await _aroRepo.GetAllAsync(a => a.MostrarEnLanding))
                    .OrderBy(a => a.Nombre).ToList();

                // Pool combinado (marcados, ordenados) — para "Disponibles" de CARRUSEL
                var poolMarcados = todosProductosMarcados
                    .Select(p => new { Id = p.Id, Tipo = "P", Nombre = p.Nombre, RutaImagen = p.RutaImagen, SKU = (string?)null, Precio = (decimal?)p.PrecioPublico })
                    .Concat(todosArosMarcados.Select(a => new { Id = a.Id, Tipo = "A", Nombre = a.Nombre, RutaImagen = a.RutaImagen, SKU = a.SKU, Precio = (decimal?)a.Precio }))
                    .ToList();

                // ── CARRUSEL — Disponibles (marcados, fuera del carrusel)
                // Excluir items que ya están en CarruselItems (respetando Tipo)
                var carruselDispFull = poolMarcados
                    .Where(x => !(x.Tipo == "P" && idsEnCarruselProducto.Contains(x.Id)))
                    .Where(x => !(x.Tipo == "A" && idsEnCarruselAro.Contains(x.Id)))
                    .ToList();
                var carruselDisp = Paginar(carruselDispFull, carruselDispPage, carruselDispPageSize);

                // ── CARRUSEL — Seleccionados (los que están en CarruselItems)
                var carruselSelFull = carrusel
                    .Select(c => new
                    {
                        Id = c.ProductoId ?? c.AroId ?? 0,
                        Tipo = c.Tipo == "Producto" ? "P" : "A",
                        Nombre = (c.Producto?.Nombre ?? c.Aro?.Nombre) ?? "",
                        RutaImagen = c.Producto?.RutaImagen ?? c.Aro?.RutaImagen
                    })
                    .ToList();
                var carruselSel = Paginar(carruselSelFull, carruselSelPage, carruselSelPageSize);

                // ── DESTACADOS — Disponibles (marcados, fuera de destacados)
                var destDispFull = poolMarcados
                    .Where(x => !(x.Tipo == "P" && idsEnDestProducto.Contains(x.Id)))
                    .Where(x => !(x.Tipo == "A" && idsEnDestAro.Contains(x.Id)))
                    .ToList();
                var destacadosDisp = Paginar(destDispFull, destacadosDispPage, destacadosDispPageSize);

                // ── DESTACADOS — Seleccionados (los que están en DestacadosItems)
                var destSelFull = destacados
                    .Select(d => new
                    {
                        Id = d.ProductoId ?? d.AroId ?? 0,
                        Tipo = d.Tipo == "Producto" ? "P" : "A",
                        Nombre = (d.Producto?.Nombre ?? d.Aro?.Nombre) ?? "",
                        RutaImagen = d.Producto?.RutaImagen ?? d.Aro?.RutaImagen
                    })
                    .ToList();
                var destacadosSel = Paginar(destSelFull, destacadosSelPage, destacadosSelPageSize);

                ViewBag.CarruselDisp = carruselDisp.Items;
                ViewBag.CarruselDispTotal = carruselDisp.TotalCount;
                ViewBag.CarruselDispPage = carruselDisp.PageIndex;
                ViewBag.CarruselDispTotalPages = carruselDisp.TotalPages;
                ViewBag.CarruselDispPageSize = carruselDispPageSize;

                ViewBag.CarruselSel = carruselSel.Items;
                ViewBag.CarruselSelTotal = carruselSel.TotalCount;
                ViewBag.CarruselSelPage = carruselSel.PageIndex;
                ViewBag.CarruselSelTotalPages = carruselSel.TotalPages;
                ViewBag.CarruselSelPageSize = carruselSelPageSize;

                ViewBag.DestacadosDisp = destacadosDisp.Items;
                ViewBag.DestacadosDispTotal = destacadosDisp.TotalCount;
                ViewBag.DestacadosDispPage = destacadosDisp.PageIndex;
                ViewBag.DestacadosDispTotalPages = destacadosDisp.TotalPages;
                ViewBag.DestacadosDispPageSize = destacadosDispPageSize;

                ViewBag.DestacadosSel = destacadosSel.Items;
                ViewBag.DestacadosSelTotal = destacadosSel.TotalCount;
                ViewBag.DestacadosSelPage = destacadosSel.PageIndex;
                ViewBag.DestacadosSelTotalPages = destacadosSel.TotalPages;
                ViewBag.DestacadosSelPageSize = destacadosSelPageSize;
            }

            return View(productos);
        }

        /// <summary>Paginador genérico para listas anónimas (las 4 listas de landing).</summary>
        private class PagedAnon
        {
            public List<object> Items { get; set; } = new();
            public int PageIndex { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
            public int PageSize { get; set; }
        }

        private PagedAnon Paginar<T>(List<T> source, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 12 : pageSize;
            var skip = (page - 1) * pageSize;
            var items = source.Skip(skip).Take(pageSize).Cast<object>().ToList();
            return new PagedAnon
            {
                Items = items,
                PageIndex = page,
                TotalCount = source.Count,
                PageSize = pageSize
            };
        }

        public IActionResult Create(string seccion = "productos")
        {
            ViewBag.Seccion = seccion;
            ViewBag.MaxImagenes = MaxImagenesPorProducto;
// categoria eliminada

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
            List<IFormFile>? imagenesProducto)
        {
            // Si el form no usa List, intentar recuperar archivos manualmente
            if (imagenesProducto == null || imagenesProducto.Count == 0)
            {
                var archivos = Request.Form.Files.GetFiles("imagenesProducto");
                if (archivos.Count > 0) imagenesProducto = archivos.ToList();
            }

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

            // Validar imágenes (si las hay)
            if (imagenesProducto != null && imagenesProducto.Count > 0)
            {
                if (imagenesProducto.Count > MaxImagenesPorProducto)
                {
                    ModelState.AddModelError("imagenesProducto",
                        $"Máximo {MaxImagenesPorProducto} imágenes por producto.");
                }
                else
                {
                    foreach (var img in imagenesProducto.Where(i => i.Length > 0))
                    {
                        var err = ValidarImagen(img);
                        if (err != null)
                        {
                            ModelState.AddModelError("imagenesProducto", err);
                            break;
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Seccion = "productos";
                ViewBag.MaxImagenes = MaxImagenesPorProducto;
                return View(model);
            }

            model.SKU = skuNorm;
            model.Activo = true;
            model.RutaImagen = null; // Ya no se usa, queda por compat

            await _productoRepo.AddAsync(model);

            // Guardar imágenes si se subieron
            if (imagenesProducto != null && imagenesProducto.Count > 0)
            {
                int orden = 1;
                bool primeraEsPrincipal = true;
                foreach (var img in imagenesProducto.Where(i => i.Length > 0))
                {
                    var ruta = await GuardarImagenProductoAsync(img);
                    var entidad = new ProductoImagen
                    {
                        ProductoId = model.Id,
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

            TempData["Success"] = "Producto registrado correctamente.";
            return RedirectToAction(nameof(Index), new { seccion = "productos" });
        }

        public async Task<IActionResult> Details(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        public async Task<IActionResult> Edit(int id, string seccion = "productos")
        {
            ViewBag.Seccion = seccion;
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();

            var imagenes = await _imagenRepo.GetAllByProductoIdAsync(id);
            ViewBag.Imagenes = imagenes;
            ViewBag.MaxImagenes = MaxImagenesPorProducto;

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
            List<IFormFile>? imagenesProducto,
            int? imagenPrincipalId,
            string? ordenImagenes)
        {
            if (imagenesProducto == null || imagenesProducto.Count == 0)
            {
                var archivos = Request.Form.Files.GetFiles("imagenesProducto");
                if (archivos.Count > 0) imagenesProducto = archivos.ToList();
            }

            var existente = await _productoRepo.GetByIdAsync(model.Id);
            if (existente == null)
                return NotFound();

            var imagenesActuales = await _imagenRepo.GetActivasByProductoIdAsync(model.Id);

            // 1) Detectar imágenes marcadas para eliminar (soft delete)
            var idsAEliminar = new HashSet<int>();
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("eliminarImagen_")))
            {
                if (int.TryParse(Request.Form[key], out var idImg))
                    idsAEliminar.Add(idImg);
            }

            // 2) Validar tope si se suben imágenes nuevas
            int totalTrasSubida = imagenesActuales.Count - idsAEliminar.Count;
            if (imagenesProducto != null && imagenesProducto.Count > 0)
            {
                if (totalTrasSubida + imagenesProducto.Count > MaxImagenesPorProducto)
                {
                    ModelState.AddModelError("imagenesProducto",
                        $"Máximo {MaxImagenesPorProducto} imágenes por producto. Tras borrar tiene {totalTrasSubida} activas y quiere subir {imagenesProducto.Count}.");
                }
                else
                {
                    foreach (var img in imagenesProducto.Where(i => i.Length > 0))
                    {
                        var err = ValidarImagen(img);
                        if (err != null)
                        {
                            ModelState.AddModelError("imagenesProducto", err);
                            break;
                        }
                    }
                }
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

                var imagenesParaRepintar = await _imagenRepo.GetAllByProductoIdAsync(model.Id);
                ViewBag.Imagenes = imagenesParaRepintar;
                ViewBag.MaxImagenes = MaxImagenesPorProducto;
                ViewBag.Seccion = "productos";
                return View(existente);
            }

            // Actualizar datos del producto
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

            await _productoRepo.UpdateAsync(existente);

            // 3) Aplicar soft delete
            foreach (var idImg in idsAEliminar)
            {
                await _imagenRepo.SoftDeleteAsync(idImg);
            }

            // 4) Subir nuevas imágenes (la primera subida nueva se marca como principal
            //    solo si NO se eligió explícitamente otra principal y no hay ninguna activa)
            if (imagenesProducto != null && imagenesProducto.Count > 0)
            {
                int orden = imagenesActuales.Count - idsAEliminar.Count + 1;
                foreach (var img in imagenesProducto.Where(i => i.Length > 0))
                {
                    var ruta = await GuardarImagenProductoAsync(img);
                    var entidad = new ProductoImagen
                    {
                        ProductoId = existente.Id,
                        Ruta = ruta,
                        Orden = orden++,
                        EsPrincipal = false,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };
                    await _imagenRepo.AddAsync(entidad);
                }
            }

            // 5) Marcar principal (si el usuario eligió una)
            if (imagenPrincipalId.HasValue && imagenPrincipalId.Value > 0)
            {
                // Verificar que la imagen pertenece al producto y está activa
                var img = await _imagenRepo.GetByIdAsync(imagenPrincipalId.Value);
                if (img != null && img.ProductoId == existente.Id && img.Activo)
                {
                    await _imagenRepo.MarcarPrincipalAsync(imagenPrincipalId.Value);
                }
            }
            else
            {
                // Si el usuario NO eligió principal pero había alguna marcada y se eliminó,
                // el repo MarcarPrincipalAsync ya desmarca las demás al setear. Pero si
                // tras los soft deletes NO quedó ninguna marcada como principal, marcamos
                // la primera activa restante.
                var principalActual = await _imagenRepo.GetPrincipalAsync(existente.Id);
                if (principalActual == null)
                {
                    var activas = await _imagenRepo.GetActivasByProductoIdAsync(existente.Id);
                    if (activas.Count > 0)
                    {
                        await _imagenRepo.MarcarPrincipalAsync(activas[0].Id);
                    }
                }
            }

            // 6) Actualizar orden si vino en el form
            //    Formato esperado: ordenImagenes = "id1:orden1,id2:orden2,..."
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
            return RedirectToAction(nameof(Index), new { seccion = "productos" });
        }

        // ============================================================
        // GESTIÓN DE IMÁGENES (soft delete -> hard delete)
        // ============================================================

        /// <summary>Pantalla de gestión: muestra activas + inactivas, permite "Eliminar definitivamente".</summary>
        [HttpGet]
        public async Task<IActionResult> GestionImagenes(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null) return NotFound();

            var imagenes = await _imagenRepo.GetAllByProductoIdAsync(id);
            ViewBag.Producto = producto;
            return View(imagenes);
        }

        /// <summary>Hard delete: borra el archivo del disco y la fila de BD.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarImagenDefinitiva(int imagenId, int productoId)
        {
            var imagen = await _imagenRepo.GetByIdAsync(imagenId);
            if (imagen == null || imagen.ProductoId != productoId)
            {
                TempData["Error"] = "Imagen no encontrada.";
                return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
            }

            // Borrar archivo del disco
            TryDeleteImagenFisica(imagen.Ruta);

            // Borrar fila de BD
            await _imagenRepo.DeleteAsync(imagenId);

            TempData["Success"] = "Imagen eliminada definitivamente.";
            return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
        }

        /// <summary>Restaurar una imagen inactiva (Activo=true).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurarImagen(int imagenId, int productoId)
        {
            var imagen = await _imagenRepo.GetByIdAsync(imagenId);
            if (imagen == null || imagen.ProductoId != productoId)
            {
                TempData["Error"] = "Imagen no encontrada.";
                return RedirectToAction(nameof(GestionImagenes), new { id = productoId });
            }

            // Verificar tope
            var activas = await _imagenRepo.CountActivasAsync(productoId);
            if (activas >= MaxImagenesPorProducto)
            {
                TempData["Error"] = $"No se puede restaurar: ya hay {activas} imágenes activas (máximo {MaxImagenesPorProducto}).";
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

        // ════════════════════════════════════════════════════════════════════════════
        //  LANDING — endpoints para guardar el orden de los slots
        //  Reciben `orden` = lista mixta de strings "P:<id>" (Producto) o "A:<id>" (Aro).
        //  Cascada: marca Destacado/MostrarEnLanding en los que entran,
        //           desmarca en los que salen.
        // ════════════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Payload: agregar=<id1>&agregar=<id2>&quitar=<id3>&tipo_<id>=P|A
        // Si el admin intenta agregar cuando ya hay 6, se rechaza (topé estricto).
        public async Task<IActionResult> GuardarCarrusel(
            [FromForm] List<int> agregar,
            [FromForm] List<int> quitar,
            [FromForm] Dictionary<string, string> tipo)
        {
            agregar ??= new List<int>();
            quitar ??= new List<int>();
            tipo ??= new Dictionary<string, string>();

            // IDs actualmente en el carrusel
            var actuales = (await _landingCarruselRepo.GetAllAsync())
                .Select(c => (Tipo: c.Tipo, Id: c.ProductoId ?? c.AroId ?? 0))
                .ToList();
            int actualesCount = actuales.Count;

            // IDs después del cambio (los agregar entran, los quitar salen)
            var idsActualesSet = new HashSet<int>(actuales.Select(a => a.Id));
            var idsEnCarruselFinal = new HashSet<int>(
                actuales.Where(a => !quitar.Contains(a.Id)).Select(a => a.Id)
                    .Concat(agregar)
            );

            if (idsEnCarruselFinal.Count > 6)
                return Json(new { ok = false, error = "El carrusel admite máximo 6 items." });

            if (idsEnCarruselFinal.Count == 0)
                return Json(new { ok = false, error = "El carrusel no puede quedar vacío si querés guardar; desmarcá todo desde Productos/Aros." });

            // Detectar si el cliente pasó un ID que NO está marcado como Destacado/MostrarEnLanding:
            // (no podemos — porque el checkbox en la vista solo aparece para items marcados)

            // Detectar conflicto: si el admin intenta agregar un ID que YA está y NO lo quitó, está OK.
            // Si quiere agregar uno nuevo pero ya hay 6, ya rechazamos arriba.

            // Construir lista final en orden: primero los existentes que NO se quitaron (mantienen Posicion),
            // después los nuevos agregar (al final).
            var itemsFinal = new List<LandingItemInput>();
            foreach (var (tipoStr, id) in actuales)
            {
                if (!quitar.Contains(id))
                    itemsFinal.Add(new LandingItemInput(tipoStr, id));
            }
            // Tipos para los nuevos agregar (vienen en `tipo_<id>`)
            foreach (var idNuevo in agregar)
            {
                if (idsActualesSet.Contains(idNuevo)) continue; // ya estaba
                var t = (tipo.TryGetValue(idNuevo.ToString(), out var tt) ? tt : "")
                    .Trim().ToUpperInvariant();
                if (t != "P" && t != "A") continue;
                itemsFinal.Add(new LandingItemInput(t == "P" ? "Producto" : "Aro", idNuevo));
            }

            if (itemsFinal.Count > 6)
                itemsFinal = itemsFinal.Take(6).ToList();

            try
            {
                await _landingCarruselRepo.ReplaceAllAsync(itemsFinal);
                await SincronizarFlagsCarruselDesdeFinal(itemsFinal);
                return Json(new { ok = true, count = itemsFinal.Count });
            }
            catch (System.Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarDestacados(
            [FromForm] List<int> agregar,
            [FromForm] List<int> quitar,
            [FromForm] Dictionary<string, string> tipo)
        {
            agregar ??= new List<int>();
            quitar ??= new List<int>();
            tipo ??= new Dictionary<string, string>();

            var actuales = (await _landingDestacadoRepo.GetAllAsync())
                .Select(d => (Tipo: d.Tipo, Id: d.ProductoId ?? d.AroId ?? 0))
                .ToList();

            var idsActualesSet = new HashSet<int>(actuales.Select(a => a.Id));
            var idsEnDestFinal = new HashSet<int>(
                actuales.Where(a => !quitar.Contains(a.Id)).Select(a => a.Id)
                    .Concat(agregar)
            );

            if (idsEnDestFinal.Count > 8)
                return Json(new { ok = false, error = "Destacados admite máximo 8 items." });

            var itemsFinal = new List<LandingItemInput>();
            foreach (var (tipoStr, id) in actuales)
            {
                if (!quitar.Contains(id))
                    itemsFinal.Add(new LandingItemInput(tipoStr, id));
            }
            foreach (var idNuevo in agregar)
            {
                if (idsActualesSet.Contains(idNuevo)) continue;
                var t = (tipo.TryGetValue(idNuevo.ToString(), out var tt) ? tt : "")
                    .Trim().ToUpperInvariant();
                if (t != "P" && t != "A") continue;
                itemsFinal.Add(new LandingItemInput(t == "P" ? "Producto" : "Aro", idNuevo));
            }

            if (itemsFinal.Count > 8)
                itemsFinal = itemsFinal.Take(8).ToList();

            try
            {
                await _landingDestacadoRepo.ReplaceAllAsync(itemsFinal);
                await SincronizarFlagsDestacadosDesdeFinal(itemsFinal);
                return Json(new { ok = true, count = itemsFinal.Count });
            }
            catch (System.Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Cascada al guardar CARRUSEL: el itemFinal ya está en BD.
        /// - Marca Destacado=true para cada Producto en el carrusel.
        /// - Marca MostrarEnLanding=true para cada Aro en el carrusel.
        /// - Items que están marcados pero NO están en CARRUSEL ni DESTACADOS: desmarca.
        /// </summary>
        private async Task SincronizarFlagsCarruselDesdeFinal(List<LandingItemInput> carruselFinal)
        {
            var destFinal = (await _landingDestacadoRepo.GetAllAsync())
                .Select(d => (Tipo: d.Tipo, Id: d.ProductoId ?? d.AroId ?? 0))
                .ToList();

            var prodEnCarrusel = new HashSet<int>(
                carruselFinal.Where(c => c.Tipo == "Producto").Select(c => c.Id));
            var aroEnCarrusel = new HashSet<int>(
                carruselFinal.Where(c => c.Tipo == "Aro").Select(c => c.Id));
            var prodEnDest = new HashSet<int>(
                destFinal.Where(d => d.Tipo == "Producto").Select(d => d.Id));
            var aroEnDest = new HashSet<int>(
                destFinal.Where(d => d.Tipo == "Aro").Select(d => d.Id));

            foreach (var id in prodEnCarrusel)
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Productos SET Destacado = 1 WHERE Id = {0}", id);
            foreach (var id in aroEnCarrusel)
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Aros SET MostrarEnLanding = 1 WHERE Id = {0}", id);

            // Limpiar flags huérfanos
            var productos = await _db.Productos.Where(p => p.Activo && p.Destacado).ToListAsync();
            foreach (var p in productos)
            {
                if (!prodEnCarrusel.Contains(p.Id) && !prodEnDest.Contains(p.Id))
                    await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Productos SET Destacado = 0 WHERE Id = {0}", p.Id);
            }
            var aros = await _db.Aros.Where(a => a.Activo && a.MostrarEnLanding).ToListAsync();
            foreach (var a in aros)
            {
                if (!aroEnCarrusel.Contains(a.Id) && !aroEnDest.Contains(a.Id))
                    await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Aros SET MostrarEnLanding = 0 WHERE Id = {0}", a.Id);
            }
        }

        private async Task SincronizarFlagsDestacadosDesdeFinal(List<LandingItemInput> destFinal)
        {
            var carruselFinal = (await _landingCarruselRepo.GetAllAsync())
                .Select(c => (Tipo: c.Tipo, Id: c.ProductoId ?? c.AroId ?? 0))
                .ToList();

            var prodEnDest = new HashSet<int>(
                destFinal.Where(d => d.Tipo == "Producto").Select(d => d.Id));
            var aroEnDest = new HashSet<int>(
                destFinal.Where(d => d.Tipo == "Aro").Select(d => d.Id));
            var prodEnCarrusel = new HashSet<int>(
                carruselFinal.Where(c => c.Tipo == "Producto").Select(c => c.Id));
            var aroEnCarrusel = new HashSet<int>(
                carruselFinal.Where(c => c.Tipo == "Aro").Select(c => c.Id));

            foreach (var id in prodEnDest)
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Productos SET Destacado = 1 WHERE Id = {0}", id);
            foreach (var id in aroEnDest)
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE Aros SET MostrarEnLanding = 1 WHERE Id = {0}", id);

            var productos = await _db.Productos.Where(p => p.Activo && p.Destacado).ToListAsync();
            foreach (var p in productos)
            {
                if (!prodEnDest.Contains(p.Id) && !prodEnCarrusel.Contains(p.Id))
                    await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Productos SET Destacado = 0 WHERE Id = {0}", p.Id);
            }
            var aros = await _db.Aros.Where(a => a.Activo && a.MostrarEnLanding).ToListAsync();
            foreach (var a in aros)
            {
                if (!aroEnDest.Contains(a.Id) && !aroEnCarrusel.Contains(a.Id))
                    await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE Aros SET MostrarEnLanding = 0 WHERE Id = {0}", a.Id);
            }
        }
    }
}
