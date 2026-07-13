using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OC.Web.ViewModels
{
    public class CrearCitaStaffViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un paciente.")]
        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una sucursal.")]
        [Display(Name = "Sucursal")]
        public int SucursalId { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
        [Display(Name = "Fecha y Hora")]
        public DateTime FechaHora { get; set; } = DateTime.Now.AddDays(1).Date.AddHours(8);

        [Display(Name = "Motivo de consulta")]
        [MaxLength(500)]
        public string? MotivoConsulta { get; set; }

        [Display(Name = "Optometrista (opcional)")]
        public int? UsuarioAsignadoId { get; set; }

        public IEnumerable<SelectListItem>? PacientesList { get; set; }
        public IEnumerable<SelectListItem>? SucursalesList { get; set; }
        public IEnumerable<SelectListItem>? OptometristasList { get; set; }
    }
}
