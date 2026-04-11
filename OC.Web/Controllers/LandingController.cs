using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http; // para sesión
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
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<Sucursal> _sucursalRepo;
        private readonly IGenericRepository<Cita> _citaRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudRepo;
        private readonly IGenericRepository<OrdenTrabajo> _ordenRepo;
        private readonly IGenericRepository<Venta> _ventaRepo;
        private readonly IGenericRepository<DetalleVenta> _detalleVentaRepo;   // ← agregado
        private readonly IGenericRepository<EnvioNotificacion> _notificacionRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;             // ← agregado

        public LandingController(
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<Sucursal> sucursalRepo,
            IGenericRepository<Cita> citaRepo,
            IGenericRepository<SolicitudCita> solicitudRepo,
            IGenericRepository<OrdenTrabajo> ordenRepo,
            IGenericRepository<Venta> ventaRepo,
            IGenericRepository<DetalleVenta> detalleVentaRepo,
            IGenericRepository<EnvioNotificacion> notificacionRepo,
            IGenericRepository<Usuario> usuarioRepo)
        {
            _productoRepo = productoRepo;
            _sucursalRepo = sucursalRepo;
            _citaRepo = citaRepo;
            _solicitudRepo = solicitudRepo;
            _ordenRepo = ordenRepo;
            _ventaRepo = ventaRepo;
            _detalleVentaRepo = detalleVentaRepo;
            _notificacionRepo = notificacionRepo;
            _usuarioRepo = usuarioRepo;
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
            var productosCarrusel = await _productoRepo.GetPagedAsync(1, 6, filter: p => p.Activo && p.Destacado && !string.IsNullOrEmpty(p.RutaImagen));
            if (!productosCarrusel.Items.Any())
                productosCarrusel = await _productoRepo.GetPagedAsync(1, 6, filter: p => p.Activo && !string.IsNullOrEmpty(p.RutaImagen));
            ViewBag.ProductosCarrusel = productosCarrusel.Items;

            var destacados = await _productoRepo.GetPagedAsync(1, 8, filter: p => p.Destacado && p.Activo);
            ViewBag.Destacados = destacados.Items;
            return View();
        }

        [AllowAnonymous]
        [Route("landing/catalogo")]
        public async Task<IActionResult> Catalogo(string? categoria, string? busqueda, int page = 1)
        {
            var productos = await _productoRepo.GetPagedAsync(
                page, 12,
                filter: p => p.Activo &&
                    (string.IsNullOrEmpty(categoria) || p.Categoria == categoria) &&
                    (string.IsNullOrEmpty(busqueda) || p.Nombre.Contains(busqueda) || p.SKU.Contains(busqueda)),
                orderBy: q => q.OrderBy(p => p.Nombre)
            );
            ViewBag.CategoriaActual = categoria;
            ViewBag.Busqueda = busqueda;
            return View(productos);
        }

        [AllowAnonymous]
        [Route("landing/detalle-producto/{id}")]
        public async Task<IActionResult> DetalleProducto(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
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
        public IActionResult Contacto() => View();

        // ========================= SECCIÓN PARA PACIENTES AUTENTICADOS =========================
        [Authorize(Roles = "Paciente")]
        [Route("landing/mis-citas")]
        public async Task<IActionResult> MisCitas()
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var citas = await _citaRepo.GetPagedAsync(1, 50,
                filter: c => c.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Sucursal"
            );
            return View(citas.Items);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/estado-orden")]
        public async Task<IActionResult> EstadoOrden()
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var ordenes = await _ordenRepo.GetPagedAsync(1, 50,
                filter: o => o.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(o => o.FechaCreacion),
                includeProperties: "Sucursal"
            );
            return View(ordenes.Items);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/mis-facturas")]
        public async Task<IActionResult> MisFacturas()
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var ventas = await _ventaRepo.GetPagedAsync(1, 50,
                filter: v => v.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(v => v.FechaVenta)
            );
            return View(ventas.Items);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/notificaciones")]
        public async Task<IActionResult> Notificaciones()
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var notificaciones = await _notificacionRepo.GetPagedAsync(1, 50,
                filter: n => n.OrdenTrabajo.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(n => n.FechaHoraEnvio),
                includeProperties: "OrdenTrabajo"
            );
            return View(notificaciones.Items);
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
        public IActionResult Carrito()
        {
            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            var carrito = string.IsNullOrEmpty(carritoJson)
                ? new List<DetalleVentaInputModel>()
                : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson);
            return View(carrito ?? new List<DetalleVentaInputModel>());
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/agregar-al-carrito")]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var producto = await _productoRepo.GetByIdAsync(productoId);
            if (producto == null || producto.Stock < cantidad)
                return Json(new { success = false, message = "Producto no disponible o stock insuficiente" });

            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            var carrito = string.IsNullOrEmpty(carritoJson)
                ? new List<DetalleVentaInputModel>()
                : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson);

            var itemExistente = carrito.FirstOrDefault(x => x.ProductoId == productoId);
            if (itemExistente != null)
            {
                if (itemExistente.Cantidad + cantidad > producto.Stock)
                    return Json(new { success = false, message = "Stock insuficiente" });
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

            HttpContext.Session.SetString("CarritoPaciente", JsonSerializer.Serialize(carrito));
            return Json(new { success = true, message = "Producto agregado al carrito" });
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [Route("landing/actualizar-carrito")]
        public IActionResult ActualizarCarrito([FromBody] List<DetalleVentaInputModel> detalles)
        {
            HttpContext.Session.SetString("CarritoPaciente", JsonSerializer.Serialize(detalles));
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/finalizar-compra")]
        public async Task<IActionResult> FinalizarCompra([FromForm] int metodoPago, [FromForm] string? notas)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            if (string.IsNullOrEmpty(carritoJson))
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            var carrito = JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson);
            if (carrito == null || !carrito.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            // Validar stock
            foreach (var item in carrito.Where(x => x.ProductoId.HasValue))
            {
                var producto = await _productoRepo.GetByIdAsync(item.ProductoId.Value);
                if (producto == null || producto.Stock < item.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente para {item.DescripcionSnapshot}";
                    return RedirectToAction(nameof(Carrito));
                }
            }

            // Obtener un usuario administrador para asignar la venta (por defecto el primero)
            var adminUser = (await _usuarioRepo.GetPagedAsync(1, 1, filter: u => u.Rol.Nombre == "Admin")).Items.FirstOrDefault();
            if (adminUser == null)
                throw new Exception("No hay usuario administrador en el sistema");

            // Crear la venta
            var venta = new Venta
            {
                NumeroFactura = GenerarNumeroFactura(),
                PacienteId = pacienteId.Value,
                UsuarioId = adminUser.Id,
                SucursalId = adminUser.SucursalId,
                MetodoPago = (MetodoPago)metodoPago,
                Notas = notas,
                FechaVenta = DateTime.Now,
                Total = carrito.Sum(x => x.Cantidad * x.PrecioUnitario)
            };
            await _ventaRepo.AddAsync(venta);

            // Crear detalles y actualizar stock
            foreach (var item in carrito)
            {
                var detalle = new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = item.ProductoId,
                    DescripcionSnapshot = item.DescripcionSnapshot,
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

            // Limpiar carrito de la sesión
            HttpContext.Session.Remove("CarritoPaciente");

            TempData["Success"] = "Compra realizada con éxito. Factura generada.";
            return RedirectToAction(nameof(Factura), new { id = venta.Id });
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/factura/{id}")]
        public async Task<IActionResult> Factura(int id)
        {
            var venta = (await _ventaRepo.GetPagedAsync(
                1, 1,
                filter: v => v.Id == id && v.PacienteId == ObtenerPacienteId(),
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
    }
}