using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Proveedor
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Contacto { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TipoProducto { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
