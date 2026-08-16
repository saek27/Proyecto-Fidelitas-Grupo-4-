using System;

namespace OC.Core.Domain.Entities
{
    /// <summary>
    /// Imagen asociada a un Aro. Mismo patrón que ProductoImagen:
    /// soporta múltiples imágenes por Aro (tope recomendado = 7),
    /// EsPrincipal indica la miniatura del catálogo, Orden el orden
    /// del carrusel, Activo=false es soft delete.
    /// </summary>
    public class AroImagen
    {
        public int Id { get; set; }
        public int AroId { get; set; }

        /// <summary>Ruta relativa bajo wwwroot (ej. /uploads/aros/abc.jpg).</summary>
        public string Ruta { get; set; } = string.Empty;

        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public Aro? Aro { get; set; }
    }
}
