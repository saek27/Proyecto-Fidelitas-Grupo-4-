namespace OC.Core.Domain.Entities
{
    public class SolicitudVacacion
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>Días calendario solicitados (inicio–fin inclusive).</summary>
        public int DiasSolicitados { get; set; }

        public string? Motivo { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public int? AprobadoPorId { get; set; }
        public Usuario? AprobadoPor { get; set; }
    }
}
