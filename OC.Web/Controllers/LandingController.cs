using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Core.Domain.Enums;
using OC.Web.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace OC.Web.Controllers
{
    public class LandingController : Controller
    {
        private const decimal TasaIvaCostaRica = 0.13m;
        private static readonly JsonSerializerOptions CarritoJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<Aro> _aroRepo;
        private readonly IGenericRepository<Sucursal> _sucursalRepo;
        private readonly IGenericRepository<Cita> _citaRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudRepo;
        private readonly IGenericRepository<OrdenTrabajo> _ordenRepo;
        private readonly IGenericRepository<Venta> _ventaRepo;
        private readonly IGenericRepository<DetalleVenta> _detalleVentaRepo;   // ← agregado
        private readonly IGenericRepository<EnvioNotificacion> _notificacionRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;
        private readonly IProductoImagenRepository _imagenRepo;
        private readonly IAroImagenRepository _aroImagenRepo;
        private readonly IWebHostEnvironment _env;

        public LandingController(
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<Aro> aroRepo,
            IGenericRepository<Sucursal> sucursalRepo,
            IGenericRepository<Cita> citaRepo,
            IGenericRepository<SolicitudCita> solicitudRepo,
            IGenericRepository<OrdenTrabajo> ordenRepo,
            IGenericRepository<Venta> ventaRepo,
            IGenericRepository<DetalleVenta> detalleVentaRepo,
            IGenericRepository<EnvioNotificacion> notificacionRepo,
            IGenericRepository<Usuario> usuarioRepo,
            IProductoImagenRepository imagenRepo,
            IAroImagenRepository aroImagenRepo,
            IWebHostEnvironment env)
        {
            _productoRepo = productoRepo;
            _aroRepo = aroRepo;
            _sucursalRepo = sucursalRepo;
            _citaRepo = citaRepo;
            _solicitudRepo = solicitudRepo;
            _ordenRepo = ordenRepo;
            _ventaRepo = ventaRepo;
            _detalleVentaRepo = detalleVentaRepo;
            _notificacionRepo = notificacionRepo;
            _usuarioRepo = usuarioRepo;
            _imagenRepo = imagenRepo;
            _aroImagenRepo = aroImagenRepo;
            _env = env;
        }

        // Redirigir trabajadores que accedan al landing por error
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            if (User.Identity.IsAuthenticated && !User.IsInRole("Paciente"))
            {
                context.Result = RedirectToAction("Index", "Home");
            }
            base.OnActionExecuting(context);
        }

        // ========================= PÁGINAS PÚBLICAS =========================
        [AllowAnonymous]
        [Route("landing")]
        [Route("landing/index")]
        public async Task<IActionResult> Index()
        {
            // Carrusel principal (top 6) = Productos destacados + Aros con MostrarEnLanding=true.
            // Si no hay productos destacados pero sí aros, mostrar aros.
            var productosDestProd = (await _productoRepo.GetPagedAsync(1, 6, filter: p => p.Activo && p.Destacado)).Items;
            if (productosDestProd.Count == 0)
                productosDestProd = (await _productoRepo.GetPagedAsync(1, 6, filter: p => p.Activo)).Items;

            var arosDestProd = (await _aroRepo.GetPagedAsync(1, 6, filter: a => a.Activo && a.MostrarEnLanding)).Items;

            // Mezclar alternando Productos y Aros, completar hasta el límite.
            var carruselMix = IntercalarLimitado<LandingItemVm>(
                productosDestProd.Select(p => new LandingItemVm { Tipo = "Producto", Id = p.Id, Nombre = p.Nombre, Descripcion = p.DescripcionCorta, Precio = p.PrecioPublico, RutaImagenLegacy = p.RutaImagen, DetalleUrl = $"/landing/detalle-producto/{p.Id}" }).ToList(),
                arosDestProd.Select(a => new LandingItemVm { Tipo = "Aro", Id = a.Id, Nombre = a.Nombre, Descripcion = a.SKU, Precio = a.Precio, RutaImagenLegacy = a.RutaImagen, DetalleUrl = $"/landing/detalle-aro/{a.Id}" }).ToList(),
                6
            );
            ViewBag.CarruselMix = carruselMix;

            // Productos Destacados (top 8) — mismo criterio
            var productosDest = (await _productoRepo.GetPagedAsync(1, 8, filter: p => p.Activo && p.Destacado)).Items;
            var arosDest = (await _aroRepo.GetPagedAsync(1, 8, filter: a => a.Activo && a.MostrarEnLanding)).Items;

            var destacadosMix = IntercalarLimitado<LandingItemVm>(
                productosDest.Select(p => new LandingItemVm { Tipo = "Producto", Id = p.Id, Nombre = p.Nombre, Descripcion = p.DescripcionCorta, Precio = p.PrecioPublico, RutaImagenLegacy = p.RutaImagen, DetalleUrl = $"/landing/detalle-producto/{p.Id}" }).ToList(),
                arosDest.Select(a => new LandingItemVm { Tipo = "Aro", Id = a.Id, Nombre = a.Nombre, Descripcion = a.SKU, Precio = a.Precio, RutaImagenLegacy = a.RutaImagen, DetalleUrl = $"/landing/detalle-aro/{a.Id}" }).ToList(),
                8
            );
            ViewBag.DestacadosMix = destacadosMix;

            // Dict de imagen principal para IDs de Productos
            var idsProd = carruselMix.Where(i => i.Tipo == "Producto").Select(i => i.Id)
                .Concat(destacadosMix.Where(i => i.Tipo == "Producto").Select(i => i.Id))
                .Distinct()
                .ToList();
            ViewBag.ImagenesPorProducto = await CargarImagenesPrincipalesAsync(idsProd);

            // Dict de imagen principal para IDs de Aros
            var idsAro = carruselMix.Where(i => i.Tipo == "Aro").Select(i => i.Id)
                .Concat(destacadosMix.Where(i => i.Tipo == "Aro").Select(i => i.Id))
                .Distinct()
                .ToList();
            ViewBag.ImagenesPorAro = await CargarImagenesPrincipalesArosAsync(idsAro);

            return View();
        }

        [AllowAnonymous]
        [Route("landing/catalogo")]
        public async Task<IActionResult> Catalogo(string? categoria, string? busqueda, int page = 1)
        {
            // "Lentes Graduados" es la categoría del landing que apunta a AROS, no a Productos.
            // (Productos NO debe tener lentes — son solo accesorios/lentes de sol.)
            if (categoria == "Lentes Graduados")
            {
                var arosPaged = await _aroRepo.GetPagedAsync(
                    page, 12,
                    filter: a => a.Activo && a.MostrarEnLanding &&
                        (string.IsNullOrEmpty(busqueda) || a.Nombre.Contains(busqueda) || a.SKU.Contains(busqueda)),
                    orderBy: q => q.OrderBy(a => a.Nombre)
                );
                ViewBag.CategoriaActual = categoria;
                ViewBag.Busqueda = busqueda;
                ViewBag.EsCategoriaAros = true;
                ViewBag.ImagenesPorAro = await CargarImagenesPrincipalesArosAsync(arosPaged.Items.Select(a => a.Id).ToList());
                return View("Catalogo", arosPaged);
            }

            var productos = await _productoRepo.GetPagedAsync(
                page, 12,
                filter: p => p.Activo &&
                    (string.IsNullOrEmpty(categoria) || p.Categoria == categoria) &&
                    (string.IsNullOrEmpty(busqueda) || p.Nombre.Contains(busqueda) || p.SKU.Contains(busqueda)),
                orderBy: q => q.OrderBy(p => p.Nombre)
            );
            ViewBag.CategoriaActual = categoria;
            ViewBag.Busqueda = busqueda;
            ViewBag.EsCategoriaAros = false;
            ViewBag.ImagenesPorProducto = await CargarImagenesPrincipalesAsync(productos.Items.Select(p => p.Id).ToList());
            return View("Catalogo", productos);
        }

        [AllowAnonymous]
        [Route("landing/detalle-producto/{id}")]
        public async Task<IActionResult> DetalleProducto(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null) return NotFound();

            var imagenes = await _imagenRepo.GetActivasByProductoIdAsync(id);
            // Si la tabla nueva no tiene filas (caso edge pre-migración), caer al fallback
            // de la ruta legacy en Productos.RutaImagen
            if ((imagenes == null || imagenes.Count == 0) && !string.IsNullOrEmpty(producto.RutaImagen))
            {
                imagenes = new List<ProductoImagen>
                {
                    new ProductoImagen
                    {
                        ProductoId = producto.Id,
                        Ruta = producto.RutaImagen,
                        EsPrincipal = true,
                        Orden = 0,
                        Activo = true
                    }
                };
            }
            ViewBag.Imagenes = imagenes;
            return View(producto);
        }

        [AllowAnonymous]
        [Route("landing/detalle-aro/{id}")]
        public async Task<IActionResult> DetalleAro(int id)
        {
            var aro = await _aroRepo.GetByIdAsync(id);
            if (aro == null) return NotFound();

            var imagenes = await _aroImagenRepo.GetActivasByAroIdAsync(id);
            if ((imagenes == null || imagenes.Count == 0) && !string.IsNullOrEmpty(aro.RutaImagen))
            {
                imagenes = new List<AroImagen>
                {
                    new AroImagen
                    {
                        AroId = aro.Id,
                        Ruta = aro.RutaImagen,
                        EsPrincipal = true,
                        Orden = 0,
                        Activo = true
                    }
                };
            }
            ViewBag.Imagenes = imagenes;
            return View(aro);
        }

        private async Task<Dictionary<int, string>> CargarImagenesPrincipalesArosAsync(List<int> aroIds)
        {
            var resultado = new Dictionary<int, string>();
            foreach (var id in aroIds)
            {
                var principal = await _aroImagenRepo.GetPrincipalAsync(id);
                if (principal != null) resultado[id] = principal.Ruta;
            }
            return resultado;
        }

        /// <summary>
        /// Intercala dos listas (Productos y Aros) para que aparezcan balanceados en el carrusel/destacados.
        /// Ej: [Prod1, Prod2, ..., Aro1, Aro2, ...] → [Prod1, Aro1, Prod2, Aro2, Prod3, Aro3, ...]
        /// Toma hasta `limite` items totales. Si una lista se agota antes, continúa con la otra.
        /// </summary>
        private static List<T> IntercalarLimitado<T>(List<T> listaA, List<T> listaB, int limite)
        {
            var resultado = new List<T>(limite);
            int maxLen = Math.Max(listaA.Count, listaB.Count);
            for (int i = 0; i < maxLen && resultado.Count < limite; i++)
            {
                if (i < listaA.Count) resultado.Add(listaA[i]);
                if (resultado.Count >= limite) break;
                if (i < listaB.Count) resultado.Add(listaB[i]);
            }
            return resultado;
        }

        [AllowAnonymous]
        [Route("landing/sucursales")]
        public async Task<IActionResult> Sucursales()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(1, 100, filter: s => s.Activo);
            return View(sucursales.Items);
        }

        [AllowAnonymous]
        [Route("landing/tecnologias")]
        public IActionResult Tecnologias() => View();

        [AllowAnonymous]
        [Route("landing/contacto")]
        public async Task<IActionResult> Contacto()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(
                1, 200,
                filter: s => s.Activo,
                orderBy: q => q.OrderBy(s => s.Nombre));
            return View(sucursales.Items);
        }

        // ========================= SECCIÓN PARA PACIENTES AUTENTICADOS =========================
        [Authorize(Roles = "Paciente")]
        [Route("landing/mis-citas")]
        public async Task<IActionResult> MisCitas(int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var citas = await _citaRepo.GetPagedAsync(page, 10,
                filter: c => c.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Sucursal"
            );
            return View(citas);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/estado-orden")]
        public async Task<IActionResult> EstadoOrden(int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var ordenes = await _ordenRepo.GetPagedAsync(page, 10,
                filter: o => o.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(o => o.FechaCreacion),
                includeProperties: "Sucursal"
            );
            return View(ordenes);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/mis-facturas")]
        public async Task<IActionResult> MisFacturas(DateTime? desde, DateTime? hasta, int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            var fechaDesde = desde?.Date;
            var fechaHasta = hasta?.Date.AddDays(1).AddTicks(-1);

            var ventas = await _ventaRepo.GetPagedAsync(page, 9,
                filter: v => v.PacienteId == pacienteId
                             && (!fechaDesde.HasValue || v.FechaVenta >= fechaDesde.Value)
                             && (!fechaHasta.HasValue || v.FechaVenta <= fechaHasta.Value),
                orderBy: q => q.OrderByDescending(v => v.FechaVenta),
                includeProperties: "Detalles,Sucursal"
            );

            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            return View(ventas);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/notificaciones")]
        public async Task<IActionResult> Notificaciones(int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var notificaciones = await _notificacionRepo.GetPagedAsync(page, 10,
                filter: n => n.OrdenTrabajo.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(n => n.FechaHoraEnvio),
                includeProperties: "OrdenTrabajo"
            );
            return View(notificaciones);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/agendar-cita")]
        public async Task<IActionResult> AgendarCita()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(1, 100, filter: s => s.Activo);
            ViewBag.SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList();
            ViewBag.ServiciosList = new[]
            {
                new SelectListItem { Value = "Examen visual", Text = "Examen visual" },
                new SelectListItem { Value = "Control de lentes", Text = "Control de lentes" },
                new SelectListItem { Value = "Adaptación de lentes de contacto", Text = "Adaptación de lentes de contacto" },
                new SelectListItem { Value = "Consulta general", Text = "Consulta general" }
            };
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/agendar-cita")]
        public async Task<IActionResult> AgendarCita(int sucursalId, string fecha, string hora, string servicio, string? motivo)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            if (sucursalId <= 0 || string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(hora) || string.IsNullOrEmpty(servicio))
            {
                TempData["Error"] = "Complete todos los campos obligatorios.";
                return RedirectToAction(nameof(AgendarCita));
            }

            if (!DateTime.TryParse(fecha, out var fechaDate))
            {
                TempData["Error"] = "Fecha no válida.";
                return RedirectToAction(nameof(AgendarCita));
            }

            if (!TimeSpan.TryParse(hora, out var horaTime))
            {
                TempData["Error"] = "Hora no válida.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var fechaHora = fechaDate.Date.Add(horaTime);
            if (fechaHora <= DateTime.Now)
            {
                TempData["Error"] = "La fecha y hora deben ser posteriores a la actual.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var ocupado = await _citaRepo.GetPagedAsync(1, 1,
                filter: c => c.SucursalId == sucursalId && c.FechaHora == fechaHora && c.Estado != EstadoCita.Cancelada);
            if (ocupado.Items.Any())
            {
                TempData["Error"] = "El horario seleccionado ya no está disponible.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var solicitud = new SolicitudCita
            {
                PacienteId = pacienteId.Value,
                Motivo = $"{servicio}. {motivo}",
                FechaSolicitud = DateTime.Now,
                Estado = "Aprobada"
            };
            await _solicitudRepo.AddAsync(solicitud);

            var cita = new Cita
            {
                PacienteId = pacienteId.Value,
                SolicitudCitaId = solicitud.Id,
                SucursalId = sucursalId,
                FechaHora = fechaHora,
                MotivoConsulta = $"{servicio}. {motivo}",
                Estado = EstadoCita.Confirmada,
                FechaCreacion = DateTime.Now
            };
            await _citaRepo.AddAsync(cita);

            TempData["Success"] = "Cita agendada correctamente.";
            return RedirectToAction(nameof(MisCitas));
        }

        [HttpGet]
        [Authorize(Roles = "Paciente")]
        [Route("landing/obtener-horas-disponibles")]
        public async Task<IActionResult> ObtenerHorasDisponibles(int sucursalId, string fecha)
        {
            if (sucursalId <= 0 || string.IsNullOrEmpty(fecha))
                return Json(Array.Empty<string>());

            if (!DateTime.TryParse(fecha, out var fechaDate))
                return Json(Array.Empty<string>());

            var horaInicio = 8;
            var horaFin = 18;
            var inicioDia = fechaDate.Date.AddHours(horaInicio);
            var finDia = fechaDate.Date.AddHours(horaFin);

            var citasOcupadas = await _citaRepo.GetPagedAsync(1, 100,
                filter: c => c.SucursalId == sucursalId
                    && c.FechaHora >= inicioDia
                    && c.FechaHora < finDia
                    && c.Estado != EstadoCita.Cancelada
            );

            var slotsOcupados = citasOcupadas.Items
                .Select(c => c.FechaHora.ToString("HH:mm"))
                .ToHashSet();

            var disponibles = new List<string>();
            for (int h = horaInicio; h < horaFin; h++)
            {
                if (!slotsOcupados.Contains($"{h:D2}:00")) disponibles.Add($"{h:D2}:00");
                if (!slotsOcupados.Contains($"{h:D2}:30")) disponibles.Add($"{h:D2}:30");
            }
            return Json(disponibles);
        }

        // ========================= CARRITO DE COMPRAS =========================
        [Authorize(Roles = "Paciente")]
        [Route("landing/carrito")]
        public async Task<IActionResult> Carrito()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(
                1, 200,
                filter: s => s.Activo,
                orderBy: q => q.OrderBy(s => s.Nombre));
            ViewBag.Sucursales = sucursales.Items;

            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            var carrito = string.IsNullOrEmpty(carritoJson)
                ? new List<DetalleVentaInputModel>()
                : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson, CarritoJsonOptions) ?? new List<DetalleVentaInputModel>();
            return View(carrito);
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/agregar-al-carrito")]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var producto = await _productoRepo.GetByIdAsync(productoId);
            if (producto == null || producto.Stock < cantidad)
            {
                TempData["Error"] = "Producto no disponible o stock insuficiente.";
                return RedirectToAction(nameof(DetalleProducto), new { id = productoId });
            }

            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            var carrito = string.IsNullOrEmpty(carritoJson)
                ? new List<DetalleVentaInputModel>()
                : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson, CarritoJsonOptions) ?? new List<DetalleVentaInputModel>();

            var itemExistente = carrito.FirstOrDefault(x => x.ProductoId == productoId);
            if (itemExistente != null)
            {
                if (itemExistente.Cantidad + cantidad > producto.Stock)
                {
                    TempData["Error"] = "Stock insuficiente para la cantidad solicitada.";
                    return RedirectToAction(nameof(DetalleProducto), new { id = productoId });
                }
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new DetalleVentaInputModel
                {
                    ProductoId = productoId,
                    DescripcionSnapshot = producto.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.PrecioPublico
                });
            }

            HttpContext.Session.SetString("CarritoPaciente", JsonSerializer.Serialize(carrito, CarritoJsonOptions));
            TempData["Success"] = "Producto agregado al carrito.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [Route("landing/actualizar-carrito")]
        public IActionResult ActualizarCarrito([FromBody] List<DetalleVentaInputModel>? detalles)
        {
            detalles ??= new List<DetalleVentaInputModel>();
            HttpContext.Session.SetString("CarritoPaciente", JsonSerializer.Serialize(detalles, CarritoJsonOptions));
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/finalizar-compra")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> FinalizarCompra(
            [FromForm] int metodoPago,
            [FromForm] int sucursalId,
            [FromForm] string? notas,
            [FromForm] List<DetalleVentaInputModel>? detalles,
            IFormFile? comprobantePago)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            if (metodoPago != (int)MetodoPago.Efectivo && metodoPago != (int)MetodoPago.SINPE)
            {
                TempData["Error"] = "En la tienda en línea solo se acepta efectivo o SINPE Móvil.";
                return RedirectToAction(nameof(Carrito));
            }

            if (metodoPago == (int)MetodoPago.SINPE && (comprobantePago == null || comprobantePago.Length == 0))
            {
                TempData["Error"] = "Con SINPE Móvil debe adjuntar un comprobante (imagen o PDF).";
                return RedirectToAction(nameof(Carrito));
            }

            if (sucursalId <= 0)
            {
                TempData["Error"] = "Debe seleccionar una sucursal.";
                return RedirectToAction(nameof(Carrito));
            }

            var sucursal = await _sucursalRepo.GetByIdAsync(sucursalId);
            if (sucursal == null || !sucursal.Activo)
            {
                TempData["Error"] = "Seleccione una sucursal válida.";
                return RedirectToAction(nameof(Carrito));
            }

            if (detalles == null || !detalles.Any())
            {
                var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
                detalles = string.IsNullOrEmpty(carritoJson)
                    ? new List<DetalleVentaInputModel>()
                    : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson, CarritoJsonOptions) ?? new List<DetalleVentaInputModel>();
            }

            detalles = detalles
                .Where(d => d.ProductoId.HasValue && d.Cantidad > 0 && d.PrecioUnitario >= 0)
                .ToList();

            if (!detalles.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            foreach (var item in detalles.Where(x => x.ProductoId.HasValue))
            {
                var producto = await _productoRepo.GetByIdAsync(item.ProductoId!.Value);
                if (producto == null || producto.Stock < item.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente para {item.DescripcionSnapshot}";
                    return RedirectToAction(nameof(Carrito));
                }
            }

            var adminUser = (await _usuarioRepo.GetPagedAsync(1, 1, filter: u => u.Rol.Nombre == "Admin", includeProperties: "Rol")).Items.FirstOrDefault();
            if (adminUser == null)
                throw new InvalidOperationException("No hay usuario administrador en el sistema");

            var subtotalBase = detalles.Sum(x => x.Cantidad * x.PrecioUnitario);
            var montoIva = Math.Round(subtotalBase * TasaIvaCostaRica, 2, MidpointRounding.AwayFromZero);
            var totalConIva = subtotalBase + montoIva;

            string? rutaComprobante = null;
            if (comprobantePago != null && comprobantePago.Length > 0)
            {
                var err = ValidarComprobante(comprobantePago);
                if (err != null)
                {
                    TempData["Error"] = err;
                    return RedirectToAction(nameof(Carrito));
                }
                rutaComprobante = await GuardarComprobanteAsync(comprobantePago);
            }

            var notasFinales = string.IsNullOrWhiteSpace(notas) ? "" : notas.Trim();
            if (!string.IsNullOrEmpty(rutaComprobante))
            {
                var lineaComp = $"Comprobante adjunto: {rutaComprobante}";
                notasFinales = string.IsNullOrEmpty(notasFinales) ? lineaComp : $"{notasFinales}\n{lineaComp}";
            }

            var venta = new Venta
            {
                NumeroFactura = GenerarNumeroFactura(),
                PacienteId = pacienteId.Value,
                UsuarioId = adminUser.Id,
                SucursalId = sucursalId,
                MetodoPago = (MetodoPago)metodoPago,
                Notas = string.IsNullOrEmpty(notasFinales) ? null : notasFinales,
                FechaVenta = DateTime.Now,
                Total = totalConIva
            };
            await _ventaRepo.AddAsync(venta);

            foreach (var item in detalles)
            {
                var detalle = new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = item.ProductoId,
                    DescripcionSnapshot = item.DescripcionSnapshot ?? "",
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Cantidad * item.PrecioUnitario
                };
                await _detalleVentaRepo.AddAsync(detalle);

                if (item.ProductoId.HasValue)
                {
                    var producto = await _productoRepo.GetByIdAsync(item.ProductoId.Value);
                    producto.Stock -= item.Cantidad;
                    await _productoRepo.UpdateAsync(producto);
                }
            }

            HttpContext.Session.Remove("CarritoPaciente");

            TempData["Success"] = "Compra realizada con éxito. Factura generada.";
            return RedirectToAction(nameof(Factura), new { id = venta.Id });
        }

        private static string? ValidarComprobante(IFormFile file)
        {
            if (file.Length > 10 * 1024 * 1024)
                return "El comprobante no puede superar 10 MB.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var ok = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            if (!ok.Contains(ext))
                return "Formato no permitido. Solo JPG, JPEG, PNG, GIF, WEBP o PDF.";
            return null;
        }

        private async Task<string> GuardarComprobanteAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath ?? "", "uploads", "comprobantes-ventas");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var ok = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            if (!ok.Contains(ext)) ext = ".pdf";

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(stream);

            return "/uploads/comprobantes-ventas/" + name;
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/factura/{id}")]
        public async Task<IActionResult> Factura(int id)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            var venta = (await _ventaRepo.GetPagedAsync(
                1, 1,
                filter: v => v.Id == id && v.PacienteId == pacienteId.Value,
                includeProperties: "Paciente,Usuario,Sucursal,Detalles.Producto"
            )).Items.FirstOrDefault();

            if (venta == null) return NotFound();

            // Buscar valor clínico asociado (opcional)
            ValorClinico? valorClinico = null; // si la venta tiene ValorClinicoId, cargarlo
            var viewModel = new FacturaViewModel { Venta = venta, ValorClinico = valorClinico };
            return View(viewModel);
        }

        private string GenerarNumeroFactura()
        {
            var año = DateTime.Now.Year;
            var ultimaVenta = _ventaRepo.GetPagedAsync(1, 1, orderBy: q => q.OrderByDescending(v => v.Id)).Result.Items.FirstOrDefault();
            int correlativo = (ultimaVenta?.Id ?? 0) + 1;
            return $"FAC-{año}-{correlativo:D6}";
        }

        private int? ObtenerPacienteId()
        {
            var claim = User.FindFirstValue("PacienteId");
            return int.TryParse(claim, out int id) ? id : null;
        }

        /// <summary>
        /// Devuelve {productoId, rutaImagenPrincipal} usando la tabla ProductoImagenes
        /// (principal primero, fallback a Productos.RutaImagen legacy).
        /// </summary>
        private async Task<Dictionary<int, string>> CargarImagenesPrincipalesAsync(List<int> productoIds)
        {
            if (productoIds == null || productoIds.Count == 0)
                return new Dictionary<int, string>();

            var dict = new Dictionary<int, string>();
            foreach (var pid in productoIds)
            {
                var imagenes = await _imagenRepo.GetActivasByProductoIdAsync(pid);
                if (imagenes != null && imagenes.Count > 0)
                {
                    dict[pid] = imagenes[0].Ruta; // ya viene ordenada: principal primero
                }
            }
            return dict;
        }
    }
}