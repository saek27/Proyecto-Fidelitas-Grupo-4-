using System;

namespace OC.Core.Domain.Entities
{
    /// <summary>Tipos de notificación para citas (CIT-RF-016).</summary>
    public static class TipoNotificacionCita
    {
        public const string RecordatorioPrevio = "RecordatorioPrevio";
        public const string RecordatorioInmediato = "RecordatorioInmediato";
        public const string Cancelacion = "Cancelacion";
    }

    /// <summary>Registro de cada envío de notificación. Se registra fecha y hora de envío.</summary>
    public class EnvioNotificacion
    {
        public int Id { get; set; }
        public int CitaId { get; set; }
        /// <summary>RecordatorioPrevio, RecordatorioInmediato, Cancelacion</summary>
        public string TipoNotificacion { get; set; } = string.Empty;
        public DateTime FechaHoraEnvio { get; set; } = DateTime.Now;
        public string Canal { get; set; } = "Email";
        public string? Destinatario { get; set; }
        public string? MensajeResumen { get; set; }
        public bool Exito { get; set; } = true;

        public Cita Cita { get; set; } = null!;
    }
}
