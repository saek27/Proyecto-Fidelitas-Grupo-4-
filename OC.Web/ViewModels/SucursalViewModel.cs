using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class SucursalViewModel
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre de la Sucursal")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = null!;

    }
}
