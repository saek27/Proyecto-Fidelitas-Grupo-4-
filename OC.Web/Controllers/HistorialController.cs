using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    public class HistorialController : Controller
    {
        private readonly IGenericRepository<Cita> _citasRepo;

        public HistorialController(
            IGenericRepository<Cita> citasRepo)
        {
            _citasRepo = citasRepo;
        }

        [Authorize(Roles = "Optometrista,Admin")]
        public async Task<IActionResult> HistorialPaciente(int PacienteId)
        {
            if (PacienteId == 0)
                return Content("PacienteId vacío");

            var citas = await _citasRepo.GetPagedAsync(
                1, 100,
                c => c.PacienteId == PacienteId && c.Estado == "Atendida",
                orderBy: q => q.OrderByDescending(c => c.FechaCreacion),
                includeProperties: "Paciente"
            );

            if (!citas.Items.Any())
                return Content("No hay citas atendidas");

            return View(citas.Items);
        }

    }

}