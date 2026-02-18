using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims; // Necesario para trabajar con Roles

namespace OC.Web.ViewComponents.Sidebar
{
    public class SidebarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // OBTENER IDENTIDAD REAL: Leemos el usuario de la petición actual
            var user = HttpContext.User;
            var menuItems = new List<MenuItem>();

            // Menú Común: Todos los usuarios autenticados ven esto
            menuItems.Add(new MenuItem { Title = "Inicio", Url = "/", Icon = "bi-speedometer2" });

            // --- Lógica de visualización REAL según el Rol en la Cookie ---

            // Solo para Administradores
            if (user.IsInRole("Admin"))
            {
                menuItems.Add(new MenuItem { Title = "Sucursales", Url = "/Sucursales", Icon = "bi-building" });
                menuItems.Add(new MenuItem { Title = "Gestión Usuarios", Url = "/Usuarios", Icon = "bi-people-fill" });
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Solicitudes de Citas", Url = "/SolicitudesCitas", Icon = "bi-calendar-check" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Inventario", Url = "/Inventory", Icon = "bi-box-seam" });
                menuItems.Add(new MenuItem { Title = "Reportes Financieros", Url = "/Reports", Icon = "bi-graph-up" });
                menuItems.Add(new MenuItem { Title = "Consultas", Url = "/Consultations", Icon = "bi-clipboard2-pulse" });
                menuItems.Add(new MenuItem { Title = "Historial Médico", Url = "/Historial/HistorialPaciente", Icon = "bi-clipboard2-pulse" });
                menuItems.Add(new MenuItem{Title = "Proveedores", Url = "/Proveedores", Icon = "bi-truck"});
            }

            // Para Admin o Personal Médico
            if (user.IsInRole("Optometrista"))
            {
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Consultas", Url = "/Consultations", Icon = "bi-clipboard2-pulse" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Historial Médico", Url = "/CitasPublicas/HistorialPaciente", Icon = "bi-clipboard2-pulse" });
            }

            // Para Recepción
            if (user.IsInRole("Recepcion"))
            {
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Solicitudes de Citas", Url = "/SolicitudesCitas", Icon = "bi-calendar-check" });
                menuItems.Add(new MenuItem { Title = "Inventario", Url = "/Inventory", Icon = "bi-box-seam" });
                menuItems.Add(new MenuItem { Title = "Reportes Financieros", Url = "/Reports", Icon = "bi-graph-up" });
                menuItems.Add(new MenuItem { Title = "Consultas", Url = "/Consultations", Icon = "bi-clipboard2-pulse" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" });
            }

            // Para Pacientes
            if (user.IsInRole("Paciente"))
            {
                menuItems.Add(new MenuItem { Title = "Mis Citas", Url = "/PacienteDashboard", Icon = "bi-calendar-event" });
                menuItems.Add(new MenuItem { Title = "Solicitar Cita", Url = "/PacienteDashboard/SolicitarCita", Icon = "bi-calendar-plus" });
                menuItems.Add(new MenuItem { Title = "Citas", Url = "/CitasPublicas/MiHistorial", Icon = "bi-calendar-event" });
            }

            return View(menuItems);
        }
    }

    public class MenuItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
    }
}