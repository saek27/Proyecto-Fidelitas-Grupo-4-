using OC.Core.Common;
using OC.Core.Domain.Entities;

namespace OC.Web.ViewModels
{
    public class VacacionesSaldoViewModel
    {
        public int? UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public DateTime? FechaContratacion { get; set; }
        public int MesesCompletos { get; set; }
        public int DiasAcumulados { get; set; }
        public int DiasUsados { get; set; }
        public int DiasDisponibles { get; set; }
        public bool TieneFechaContratacion => FechaContratacion.HasValue;
        public bool TieneSolicitudPendiente { get; set; }
        public bool PuedeSolicitar { get; set; }
        public string? MotivoNoSolicitar { get; set; }
    }

    public class VacacionesIndexViewModel
    {
        public bool EsAdmin { get; set; }
        public VacacionesSaldoViewModel Saldo { get; set; } = new();
        public VacacionesSaldoViewModel? SaldoConsultado { get; set; }
        public PagedResult<SolicitudVacacion> Solicitudes { get; set; } = new([], 0, 1, 10);
        public int? UsuarioConsultaId { get; set; }
        public List<Usuario> UsuariosFiltro { get; set; } = [];
    }

    public class SolicitudVacacionCreateViewModel
    {
        public DateTime FechaInicio { get; set; } = DateTime.Today;
        public DateTime FechaFin { get; set; } = DateTime.Today;
        public string? Motivo { get; set; }
        public VacacionesSaldoViewModel Saldo { get; set; } = new();
    }
}
