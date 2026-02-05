using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OC.Web.ViewModels
{
    public class CitaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un paciente")]
        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una solicitud")]
        [Display(Name = "Solicitud")]
        public int SolicitudCitaId { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias")]
        [Display(Name = "Fecha y Hora")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; } = DateTime.Now.AddDays(1);

        [Display(Name = "Observaciones")]
        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres")]
        public string? Observaciones { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Programada";

        [Display(Name = "Optometrista Asignado")]
        public int? UsuarioAsignadoId { get; set; }

        // Para mostrar información
        public string? NombrePaciente { get; set; }
        public string? MotivoSolicitud { get; set; }

        // Listas para dropdowns
        public IEnumerable<SelectListItem>? OptometristasList { get; set; }
    }
}
