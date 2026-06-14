using OC.Core.Domain.Entities;

namespace OC.Web.Helpers
{
    public static class VacacionesHelper
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoAprobado = "Aprobado";
        public const string EstadoRechazado = "Rechazado";

        /// <summary>Meses completos desde la contratación (aniversario a aniversario).</summary>
        public static int ContarMesesCompletos(DateTime fechaContratacion, DateTime? referencia = null)
        {
            var hoy = (referencia ?? DateTime.Today).Date;
            var inicio = fechaContratacion.Date;
            if (hoy < inicio)
                return 0;

            var meses = 0;
            while (inicio.AddMonths(meses + 1) <= hoy)
                meses++;

            return meses;
        }

        public static int ContarDiasCalendario(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaFin.Date < fechaInicio.Date)
                return 0;
            return (fechaFin.Date - fechaInicio.Date).Days + 1;
        }

        public static int CalcularDiasAcumulados(DateTime? fechaContratacion, DateTime? referencia = null)
        {
            if (!fechaContratacion.HasValue)
                return 0;
            return ContarMesesCompletos(fechaContratacion.Value, referencia);
        }

        public static int CalcularDiasUsados(IEnumerable<SolicitudVacacion> solicitudes)
        {
            return solicitudes
                .Where(s => s.Estado == EstadoAprobado)
                .Sum(s => s.DiasSolicitados);
        }

        public static int CalcularDiasDisponibles(DateTime? fechaContratacion, IEnumerable<SolicitudVacacion> solicitudes, DateTime? referencia = null)
        {
            var acumulados = CalcularDiasAcumulados(fechaContratacion, referencia);
            var usados = CalcularDiasUsados(solicitudes);
            return Math.Max(0, acumulados - usados);
        }

        public static bool TieneSolicitudPendiente(IEnumerable<SolicitudVacacion> solicitudes)
        {
            return solicitudes.Any(s => s.Estado == EstadoPendiente);
        }
    }
}
