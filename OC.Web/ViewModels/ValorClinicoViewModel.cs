using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class ValorClinicoViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ExpedienteId { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        [Display(Name = "Diagnóstico")]
        [MaxLength(500, ErrorMessage = "El diagnóstico no puede exceder 500 caracteres.")]
        public string Diagnostico { get; set; }

        [Display(Name = "Esfera Ojo Derecho")]
        public decimal? EsferaOD { get; set; }

        [Display(Name = "Cilindro Ojo Derecho")]
        public decimal? CilindroOD { get; set; }

        [Display(Name = "Eje Ojo Derecho")]
        public decimal? EjeOD { get; set; }

        [Display(Name = "Esfera Ojo Izquierdo")]
        public decimal? EsferaOI { get; set; }

        [Display(Name = "Cilindro Ojo Izquierdo")]
        public decimal? CilindroOI { get; set; }

        [Display(Name = "Eje Ojo Izquierdo")]
        public decimal? EjeOI { get; set; }

        // Para mostrar en la vista
        public string? NombrePaciente { get; set; }
        public string? MotivoConsulta { get; set; }
    }
}