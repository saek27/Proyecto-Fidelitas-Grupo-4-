using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string NumeroSeguimiento { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Asignado, En Proceso, Resuelto, Cerrado

        [MaxLength(20)]
        public string? Prioridad { get; set; } // Baja, Media, Alta, Urgente

        [MaxLength(30)]
        public string? Tipo { get; set; } // Hardware, Software, Red, Periférico, Otro

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaAsignacion { get; set; }   // <--- NUEVA PROPIEDAD
        public DateTime? FechaCierre { get; set; }

        public int CreadoPorId { get; set; }
        public Usuario CreadoPor { get; set; } = null!;

        public int? EquipoId { get; set; }
        public Equipo? Equipo { get; set; }

        public int? TecnicoAsignadoId { get; set; }
        public Usuario? TecnicoAsignado { get; set; }

        public string? ObservacionesCierre { get; set; }
    }
}