using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using System.Security.Claims;

namespace OC.Web.ViewComponents.Notificaciones
{
    public class NotificacionesViewComponent : ViewComponent
    {
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;

        public NotificacionesViewComponent(IGenericRepository<SolicitudCita> solicitudesRepo)
        {
            _solicitudesRepo = solicitudesRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = HttpContext.User;
            int pendientesCount = 0;

            // Solo para Recepcion
            if (user.IsInRole("Recepcion"))
            {
                try
                {
                    var pendientes = await _solicitudesRepo.GetPagedAsync(
                        pageIndex: 1,
                        pageSize: 1000,
                        filter: s => s.Estado == "Pendiente"
                    );
                    pendientesCount = pendientes.TotalCount;
                }
                catch
                {
                    // Si la tabla no existe aún (migración no ejecutada), retornar 0
                    pendientesCount = 0;
                }
            }

            return View(pendientesCount);
        }
    }
}
