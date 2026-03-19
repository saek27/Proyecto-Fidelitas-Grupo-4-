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
        public int NumeroTelefonico { get; set; }

        [Required]
        public string Correo { get; set; } = string.Empty;


        public bool Activo { get; set; } = true;
    }
}
