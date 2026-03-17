using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;

namespace OC.Web.ViewComponents.Sidebar
{
    public class SidebarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var user = HttpContext.User;
            var menuItems = new List<MenuItem>();

            // Menú común para todos los autenticados
            menuItems.Add(new MenuItem { Title = "Inicio", Url = "/", Icon = "bi-speedometer2" });

            // Admin
            if (user.IsInRole("Admin"))
            {
                menuItems.Add(new MenuItem { Title = "Sucursales", Url = "/Sucursales", Icon = "bi-building" });
                menuItems.Add(new MenuItem { Title = "Gestión Usuarios", Url = "/Usuarios", Icon = "bi-people-fill" });
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Inventario", Url = "/Inventory", Icon = "bi-box-seam" });
                menuItems.Add(new MenuItem { Title = "Reportes Financieros", Url = "/Reportes", Icon = "bi-graph-up" });
                menuItems.Add(new MenuItem { Title = "Historial Médico", Url = "/Historial/HistorialPaciente", Icon = "bi-clipboard2-pulse" });
                menuItems.Add(new MenuItem { Title = "Proveedores", Url = "/Proveedores", Icon = "bi-truck" });
                menuItems.Add(new MenuItem { Title = "Historial de Pedidos", Url = "/Pedidos/Historial", Icon = "bi-clock-history" });
                menuItems.Add(new MenuItem { Title = "Nuevo Pedido", Url = "/Pedidos/Create", Icon = "bi-plus-circle" });
                menuItems.Add(new MenuItem { Title = "Ventas", Url = "/Ventas", Icon = "bi bi-receipt" });

                // Opciones de Mesa de Ayuda (se agruparán en el menú desplegable)
                menuItems.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Panel Técnico", Url = "/Tickets/PanelTecnico", Icon = "bi-tools", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Gestionar Tickets", Url = "/Tickets/Index", Icon = "bi-gear", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Equipos TI", Url = "/Equipos", Icon = "bi-pc-display", IsHelpDeskItem = true });
            }

            // Optometrista
            if (user.IsInRole("Optometrista"))
            {
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Historial Médico", Url = "/CitasPublicas/HistorialPaciente", Icon = "bi-clipboard2-pulse" });

                // Mesa de Ayuda para Optometrista
                menuItems.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket", IsHelpDeskItem = true });
            }

            // Recepcion
            if (user.IsInRole("Recepcion"))
            {
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Ventas",Url = "/Ventas",Icon = "bi bi-receipt"});
                // Mesa de Ayuda para Recepcion
                menuItems.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket", IsHelpDeskItem = true });
            }

            // Tecnico
            if (user.IsInRole("Tecnico"))
            {
                // Mesa de Ayuda para Técnico
                menuItems.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Panel Técnico", Url = "/Tickets/PanelTecnico", Icon = "bi-tools", IsHelpDeskItem = true });
                menuItems.Add(new MenuItem { Title = "Equipos TI", Url = "/Equipos", Icon = "bi-pc-display", IsHelpDeskItem = true });
            }

            // Paciente
            if (user.IsInRole("Paciente"))
            {
                menuItems.Add(new MenuItem { Title = "Mis Citas", Url = "/PacienteDashboard", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Solicitar Cita", Url = "/PacienteDashboard/SolicitarCita", Icon = "bi-calendar-plus" });
            }

            return View(menuItems);
        }
    }

    public class MenuItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public bool IsHelpDeskItem { get; set; } // Nueva propiedad para identificar ítems de Mesa de Ayuda
    }
}