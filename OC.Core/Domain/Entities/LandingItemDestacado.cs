using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    /// <summary>
    /// Una fila = un slot fijo (1..8) de la sección "Productos Destacados" del landing.
    /// El admin arrastra Productos/Aros a estos slots desde la pestaña Inventario → Landing.
    /// El orden lo define Posicion.
    /// </summary>
    public class LandingItemDestacado
    {
        public int Id { get; set; }

        [Range(1, 8)]
        public byte Posicion { get; set; }

        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = string.Empty; // "Producto" | "Aro"

        public int? ProductoId { get; set; }
        public int? AroId { get; set; }

        public Producto? Producto { get; set; }
        public Aro? Aro { get; set; }
    }
}