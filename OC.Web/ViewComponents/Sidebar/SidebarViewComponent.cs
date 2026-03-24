using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace OC.Web.ViewComponents.Sidebar
{
    public class SidebarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var user = HttpContext.User;
            var items = new List<MenuItem>();
            var helpDesk = new List<MenuItem>();

            items.Add(new MenuItem { Title = "Inicio", Url = "/", Icon = "bi-speedometer2" });

            if (user.IsInRole("Admin"))
            {
                items.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                items.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                items.Add(new MenuItem { Title = "Ventas", Url = "/Ventas", Icon = "bi-receipt" });
                items.Add(new MenuItem { Title = "Inventario", Url = "/Inventory", Icon = "bi-box-seam" });
                items.Add(new MenuItem { Title = "Pedidos", Url = "/Pedidos", Icon = "bi-truck" });

                items.Add(new MenuItem { Title = "Proveedores", Url = "/Proveedores", Icon = "bi-building-check" });
                items.Add(new MenuItem { Title = "Empleados", Url = "/Empleados", Icon = "bi-person-badge" });
                items.Add(new MenuItem { Title = "Sucursales", Url = "/Sucursales", Icon = "bi-geo-alt" });
                items.Add(new MenuItem { Title = "Usuarios", Url = "/Usuarios", Icon = "bi-shield-lock" });
                items.Add(new MenuItem { Title = "Reportes", Url = "/Reportes", Icon = "bi-bar-chart-line" });
                items.Add(new MenuItem { Title = "Órdenes Trabajo", Url = "/OrdenesTrabajo", Icon = "bi-eyeglasses" });
                items.Add(new MenuItem { Title = "Planillas", Url = "/Planillas", Icon = "bi-calculator" });

                helpDesk.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" });
                helpDesk.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" });
                helpDesk.Add(new MenuItem { Title = "Panel Técnico", Url = "/Tickets/PanelTecnico", Icon = "bi-tools" });
                helpDesk.Add(new MenuItem { Title = "Gestionar Tickets", Url = "/Tickets", Icon = "bi-gear" });
                helpDesk.Add(new MenuItem { Title = "Equipos TI", Url = "/Equipos", Icon = "bi-pc-display" });
            }

            if (user.IsInRole("Optometrista"))
            {
                items.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                items.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                items.Add(new MenuItem { Title = "Expedientes", Url = "/Expedientess", Icon = "bi-folder2-open" });
                items.Add(new MenuItem { Title = "Historial", Url = "/CitasPublicas/HistorialPaciente", Icon = "bi-clipboard2-pulse" });
                items.Add(new MenuItem { Title = "Órdenes Trabajo", Url = "/OrdenesTrabajo", Icon = "bi-eyeglasses" });
                items.Add(new MenuItem { Title = "Mis Planillas", Url = "/Planillas/MisPlanillas", Icon = "bi-file-earmark-text" });

                helpDesk.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" });
                helpDesk.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" });
            }

            if (user.IsInRole("Recepcion"))
            {
                items.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                items.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                items.Add(new MenuItem { Title = "Ventas", Url = "/Ventas", Icon = "bi-receipt" });
                items.Add(new MenuItem { Title = "Órdenes Trabajo", Url = "/OrdenesTrabajo", Icon = "bi-eyeglasses" });
                items.Add(new MenuItem { Title = "Mis Planillas", Url = "/Planillas/MisPlanillas", Icon = "bi-file-earmark-text" });

                helpDesk.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" });
                helpDesk.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" });
            }

            if (user.IsInRole("Tecnico"))
            {
                items.Add(new MenuItem { Title = "Mis Planillas", Url = "/Planillas/MisPlanillas", Icon = "bi-file-earmark-text" });

                helpDesk.Add(new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" });
                helpDesk.Add(new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" });
                helpDesk.Add(new MenuItem { Title = "Panel Técnico", Url = "/Tickets/PanelTecnico", Icon = "bi-tools" });
                helpDesk.Add(new MenuItem { Title = "Equipos TI", Url = "/Equipos", Icon = "bi-pc-display" });
            }

            if (user.IsInRole("Paciente"))
            {
                items.Add(new MenuItem { Title = "Mis Citas", Url = "/PacienteDashboard", Icon = "bi-calendar-event" });
                items.Add(new MenuItem { Title = "Solicitar Cita", Url = "/PacienteDashboard/AgendarCita", Icon = "bi-calendar-plus" });
                items.Add(new MenuItem { Title = "Estado de Orden", Url = "/PacienteDashboard/EstadoOrden", Icon = "bi-eyeglasses" });
                items.Add(new MenuItem { Title = "Mis Facturas", Url = "/PacienteDashboard/MisFacturas", Icon = "bi-receipt" });
                items.Add(new MenuItem { Title = "Notificaciones", Url = "/PacienteDashboard/Notificaciones", Icon = "bi-bell" });
            }

            return View(new SidebarViewModel { Items = items, HelpDeskItems = helpDesk });
        }
    }

    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsHelpDeskItem { get; set; }
    }

    public class SidebarViewModel
    {
        public List<MenuItem> Items { get; set; } = new();
        public List<MenuItem> HelpDeskItems { get; set; } = new();
    }
}