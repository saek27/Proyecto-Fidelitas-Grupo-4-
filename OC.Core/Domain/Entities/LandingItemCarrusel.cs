using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    /// <summary>
    /// Una fila = un slot fijo (1..6) del carrusel principal del landing.
    /// El admin arrastra Productos/Aros a estos slots desde la pestaña Inventario → Landing.
    /// El orden lo define Posicion. Los flags Destacado/MostrarEnLanding se sincronizan en cascada.
    /// </summary>
    public class LandingItemCarrusel
    {
        public int Id { get; set; }

        [Range(1, 6)]
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