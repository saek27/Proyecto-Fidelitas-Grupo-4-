using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace OC.Web.ViewComponents.Sidebar
{
    public class SidebarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // SIMULACIÓN: En producción esto vendría de User.Claims o Database
            // Roles posibles para probar: "Admin", "Optometrista", "Recepcion"
            var userRole = "Admin";

            var menuItems = new List<MenuItem>();

            // Menú Común (Todos lo ven)
            menuItems.Add(new MenuItem { Title = "Inicio", Url = "/", Icon = "bi-speedometer2" });

            // Lógica de visualización según Rol
            if (userRole == "Admin")
            {
                menuItems.Add(new MenuItem { Title = "Gestión Usuarios", Url = "/Users", Icon = "bi-people-fill" });
                menuItems.Add(new MenuItem { Title = "Inventario", Url = "/Inventory", Icon = "bi-box-seam" });
                menuItems.Add(new MenuItem { Title = "Reportes Financieros", Url = "/Reports", Icon = "bi-graph-up" });
            }

            if (userRole == "Admin" || userRole == "Optometrista")
            {
                menuItems.Add(new MenuItem { Title = "Pacientes", Url = "/Patients", Icon = "bi-person-heart" });
                menuItems.Add(new MenuItem { Title = "Consultas", Url = "/Consultations", Icon = "bi-clipboard2-pulse" });
            }

            return View(menuItems);
        }
    }

    // Clase auxiliar simple para el menú (puedes moverla a un archivo separado después)
    public class MenuItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; } // Clase de Bootstrap Icons
    }
}