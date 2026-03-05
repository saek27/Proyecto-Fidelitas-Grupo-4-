using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace OC.Web.Services
{
    /// <summary>Implementación de notificaciones de citas. Registra cada envío en EnviosNotificacion (fecha/hora). Canal Email/SMS/WhatsApp; por defecto se registra y se puede extender con envío real.</summary>
    public class NotificationService : INotificationService
    {
        private readonly IGenericRepository<EnvioNotificacion> _enviosRepo;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IGenericRepository<EnvioNotificacion> enviosRepo,
            ILogger<NotificationService> logger)
        {
            _enviosRepo = enviosRepo;
            _logger = logger;
        }

        public async Task EnviarRecordatorioPrevioAsync(Cita cita, int horasAntes, CancellationToken ct = default)
        {
            if (!cita.NotificacionesActivas) return;
            var (destinatario, mensaje) = ConstruirMensajeRecordatorio(cita, esInmediato: false);
            var registro = new EnvioNotificacion
            {
                CitaId = cita.Id,
                TipoNotificacion = TipoNotificacionCita.RecordatorioPrevio,
                FechaHoraEnvio = DateTime.Now,
                Canal = cita.CanalNotificacion,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("Recordatorio previo registrado para CitaId={CitaId}, PacienteId={PacienteId}, Canal={Canal}", cita.Id, cita.PacienteId, cita.CanalNotificacion);
            await EnviarPorCanalAsync(cita, destinatario, mensaje, "Recordatorio de cita", ct);
        }

        public async Task EnviarRecordatorioInmediatoAsync(Cita cita, CancellationToken ct = default)
        {
            if (!cita.NotificacionesActivas) return;
            var (destinatario, mensaje) = ConstruirMensajeRecordatorio(cita, esInmediato: true);
            var registro = new EnvioNotificacion
            {
                CitaId = cita.Id,
                TipoNotificacion = TipoNotificacionCita.RecordatorioInmediato,
                FechaHoraEnvio = DateTime.Now,
                Canal = cita.CanalNotificacion,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("Recordatorio inmediato registrado para CitaId={CitaId}, PacienteId={PacienteId}", cita.Id, cita.PacienteId);
            await EnviarPorCanalAsync(cita, destinatario, mensaje, "Cita agendada para hoy", ct);
        }

        public async Task EnviarNotificacionCancelacionAsync(Cita cita, CancellationToken ct = default)
        {
            if (!cita.NotificacionesActivas) return;
            var (destinatario, mensaje) = ConstruirMensajeCancelacion(cita);
            var registro = new EnvioNotificacion
            {
                CitaId = cita.Id,
                TipoNotificacion = TipoNotificacionCita.Cancelacion,
                FechaHoraEnvio = DateTime.Now,
                Canal = cita.CanalNotificacion,
                Destinatario = destinatario,
                MensajeResumen = mensaje,
                Exito = true
            };
            await _enviosRepo.AddAsync(registro);
            _logger.LogInformation("Notificación de cancelación registrada para CitaId={CitaId}, PacienteId={PacienteId}", cita.Id, cita.PacienteId);
            await EnviarPorCanalAsync(cita, destinatario, mensaje, "Cita cancelada", ct);
        }

        private static (string? Destinatario, string MensajeResumen) ConstruirMensajeRecordatorio(Cita cita, bool esInmediato)
        {
            var lugar = cita.Sucursal?.Nombre ?? "Sede";
            var fechaHora = cita.FechaHora.ToString("dd/MM/yyyy HH:mm");
            var mensaje = esInmediato
                ? $"Su cita fue agendada para hoy. Fecha y hora: {fechaHora}. Lugar: {lugar}."
                : $"Recordatorio: Tiene una cita el {fechaHora} en {lugar}.";
            var destinatario = ObtenerDestinatario(cita);
            return (destinatario, mensaje);
        }

        private static (string? Destinatario, string MensajeResumen) ConstruirMensajeCancelacion(Cita cita)
        {
            var mensaje = "Su cita ha sido cancelada y ya no se realizará. Si desea reagendar, puede solicitar una nueva cita.";
            var destinatario = ObtenerDestinatario(cita);
            return (destinatario, mensaje);
        }

        private static string? ObtenerDestinatario(Cita cita)
        {
            var p = cita.Paciente;
            if (p == null) return null;
            return cita.CanalNotificacion switch
            {
                "Email" => p.Email,
                "SMS" or "WhatsApp" => p.Telefono,
                _ => p.Email ?? p.Telefono
            };
        }

        /// <summary>Punto de extensión: envío real por Email/SMS/WhatsApp. Por defecto solo se registró en DB.</summary>
        private static Task EnviarPorCanalAsync(Cita cita, string? destinatario, string mensaje, string asunto, CancellationToken ct)
        {
            // Aquí se puede integrar SMTP, Twilio, etc. Por ahora solo registro en EnviosNotificacion.
            return Task.CompletedTask;
        }
    }
}
