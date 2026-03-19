using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class OrdenTrabajoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un paciente")]
        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una sucursal")]
        [Display(Name = "Sucursal")]
        public int SucursalId { get; set; }

        [Display(Name = "Venta (opcional)")]
        public int? VentaId { get; set; }

        [Display(Name = "Referencia")]
        [MaxLength(200)]
        public string? Referencia { get; set; }
    }
}
