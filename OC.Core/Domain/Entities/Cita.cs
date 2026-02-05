using System;

namespace OC.Core.Domain.Entities
{
    public class Cita
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public int SolicitudCitaId { get; set; }
        public DateTime FechaHora { get; set; }
        public string? Observaciones { get; set; }
        public string Estado { get; set; } = "Programada"; // Programada, Completada, Cancelada
        public int? UsuarioAsignadoId { get; set; } // Optometrista asignado
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        public Paciente Paciente { get; set; } = null!;
        public SolicitudCita SolicitudCita { get; set; } = null!;
        public Usuario? UsuarioAsignado { get; set; }
    }
}
