using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class EmpleadoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        public string Apellidos { get; set; } = null!;

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [RegularExpression(@"^(\d-\d{4}-\d{4}|\d{9})$", ErrorMessage = "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789")]
        [StringLength(11, MinimumLength = 9, ErrorMessage = "Use el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789")]
        public string Cedula { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string Telefono { get; set; } = null!;

        [Required(ErrorMessage = "El puesto es obligatorio")]
        public string Puesto { get; set; } = null!;

        [Required(ErrorMessage = "Debe seleccionar una sucursal")]
        public int SucursalId { get; set; }

    }
}
