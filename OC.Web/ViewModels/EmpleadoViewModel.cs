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
        [StringLength(9, MinimumLength = 9, ErrorMessage = "La cédula debe tener exactamente 9 dígitos. Ejemplo: 604240201")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "La cédula debe contener solo números (9 dígitos).")]
        public string Cedula { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string Telefono { get; set; } = null!;

        [Required(ErrorMessage = "El puesto es obligatorio")]
        public string Puesto { get; set; } = null!;

        [Required(ErrorMessage = "Debe seleccionar una sucursal")]
        public int SucursalId { get; set; }

    }
}
