using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Web.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
            ViewBag.Modulos = GetModulos(rol);
            return View();
        }

        private static List<ModuloItem> GetModulos(string rol) => rol switch
        {
            "Admin" => new()
            {
                new("bi-person-heart",   "Pacientes",       "Gestión de pacientes",    "/Pacientes",                   "#93c5fd", "rgba(59,130,246,.25)"),
                new("bi-calendar-event", "Citas",           "Agendar y gestionar",     "/CitasPublicas/CitasPaciente", "#67e8f9", "rgba(6,182,212,.25)"),
                new("bi-folder2-open",   "Expedientes",     "Historial clínico",       "/Expedientess",                "#c4b5fd", "rgba(139,92,246,.25)"),
                new("bi-receipt",        "Ventas",          "Registrar cobros",        "/Ventas",                      "#6ee7b7", "rgba(16,185,129,.25)"),
                new("bi-box-seam",       "Inventario",      "Productos y stock",       "/Inventory",                   "#fcd34d", "rgba(245,158,11,.25)"),
                new("bi-truck",          "Pedidos",         "Órdenes a proveedores",   "/Pedidos",                     "#fca5a5", "rgba(239,68,68,.25)"),
                new("bi-building-check", "Proveedores",     "Gestión de proveedores",  "/Proveedores",                 "#cbd5e1", "rgba(148,163,184,.2)"),
                new("bi-person-badge",   "Empleados",       "Personal de la óptica",   "/Empleados",                   "#67e8f9", "rgba(8,145,178,.25)"),
                new("bi-geo-alt",        "Sucursales",      "Gestión de sucursales",   "/Sucursales",                  "#c4b5fd", "rgba(124,58,237,.25)"),
                new("bi-shield-lock",    "Usuarios",        "Cuentas y roles",         "/Usuarios",                    "#fca5a5", "rgba(220,38,38,.25)"),
                new("bi-bar-chart-line", "Reportes",        "Análisis gerencial",      "/Reportes",                    "#6ee7b7", "rgba(5,150,105,.25)"),
                new("bi-eyeglasses",     "Órdenes Trabajo", "Seguimiento de órdenes",  "/OrdenesTrabajo",              "#93c5fd", "rgba(59,130,246,.2)"),
                new("bi-calculator",     "Planillas",       "Gestión de planillas",    "/Planillas",                   "#fcd34d", "rgba(245,158,11,.2)"),
                new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                new("bi-pc-display",     "Equipos TI",      "Inventario de equipos",   "/Equipos",                     "#94a3b8", "rgba(71,85,105,.3)"),
            },

            "Recepcion" => new()
            {
                new("bi-person-heart",   "Pacientes",       "Gestión de pacientes",    "/Pacientes",                   "#93c5fd", "rgba(59,130,246,.25)"),
                new("bi-calendar-event", "Citas",           "Agendar y gestionar",     "/CitasPublicas/CitasPaciente", "#67e8f9", "rgba(6,182,212,.25)"),
                new("bi-receipt",        "Ventas",          "Registrar cobros",        "/Ventas",                      "#6ee7b7", "rgba(16,185,129,.25)"),
                new("bi-eyeglasses",     "Órdenes Trabajo", "Seguimiento de órdenes",  "/OrdenesTrabajo",              "#93c5fd", "rgba(59,130,246,.2)"),
                new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",      "#fcd34d", "rgba(245,158,11,.2)"),
                new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
            },

            "Optometrista" => new()
            {
                new("bi-person-heart",   "Pacientes",       "Gestión de pacientes",    "/Pacientes",                        "#93c5fd", "rgba(59,130,246,.25)"),
                new("bi-calendar-event", "Citas",           "Agendar y gestionar",     "/CitasPublicas/CitasPaciente",      "#67e8f9", "rgba(6,182,212,.25)"),
                new("bi-folder2-open",   "Expedientes",     "Historial clínico",       "/Expedientess",                     "#c4b5fd", "rgba(139,92,246,.25)"),
                new("bi-eyeglasses",     "Órdenes Trabajo", "Seguimiento de órdenes",  "/OrdenesTrabajo",                   "#93c5fd", "rgba(59,130,246,.2)"),
                new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",           "#fcd34d", "rgba(245,158,11,.2)"),
                new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",               "#fcd34d", "rgba(217,119,6,.25)"),
            },

            "Tecnico" => new()
            {
                new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",      "#fcd34d", "rgba(245,158,11,.2)"),
                new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                new("bi-pc-display",     "Equipos TI",      "Inventario de equipos",   "/Equipos",                     "#94a3b8", "rgba(71,85,105,.3)"),
            },

            "Paciente" => new()
            {
                new("bi-calendar-event", "Mis Citas",       "Ver y gestionar citas",   "/PacienteDashboard",                "#93c5fd", "rgba(59,130,246,.25)"),
                new("bi-calendar-plus",  "Solicitar Cita",  "Nueva solicitud",         "/PacienteDashboard/AgendarCita",    "#67e8f9", "rgba(6,182,212,.25)"),
                new("bi-eyeglasses",     "Estado de Orden", "Ver mis lentes",          "/PacienteDashboard/EstadoOrden",    "#c4b5fd", "rgba(139,92,246,.25)"),
                new("bi-receipt",        "Mis Facturas",    "Historial de compras",    "/PacienteDashboard/MisFacturas",    "#6ee7b7", "rgba(16,185,129,.25)"),
                new("bi-bell",           "Notificaciones",  "Mis avisos",              "/PacienteDashboard/Notificaciones", "#fcd34d", "rgba(245,158,11,.25)"),
            },

            _ => new()
        };

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public record ModuloItem(
        string Icon,
        string Label,
        string Sub,
        string Url,
        string Color,
        string Bg
    );
}