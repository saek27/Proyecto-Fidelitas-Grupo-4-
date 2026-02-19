using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class ValorClinico
    {
        public int Id { get; set; }

        [Required]
        public int ExpedienteId { get; set; }
        public Expediente Expediente { get; set; }

        [Required]
        public string Diagnostico { get; set; }

        public decimal? EsferaOD { get; set; }
        public decimal? CilindroOD { get; set; }
        public decimal? EjeOD { get; set; }

        public decimal? EsferaOI { get; set; }
        public decimal? CilindroOI { get; set; }
        public decimal? EjeOI { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}