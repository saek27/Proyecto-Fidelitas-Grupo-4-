using System;

namespace OC.Core.Domain.Entities
{
    /// <summary>
    /// Imagen asociada a un Producto. Soporta hasta N imágenes por producto
    /// (controlado en la UI/Controller, tope recomendado = 7).
    ///
    /// - EsPrincipal = 1 indica la imagen que se muestra como miniatura en el catálogo
    ///   y como primer slide en el carrusel del detalle.
    /// - Orden define el orden visual de las imágenes restantes (1..N).
    /// - Activo = 0 es "soft delete": la imagen no aparece en landing/catálogo
    ///   pero el archivo sigue en disco hasta que el admin lo borre definitivamente
    ///   desde /Inventory/GestionImagenes/{productoId}.
    /// </summary>
    public class ProductoImagen
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }

        /// <summary>Ruta relativa bajo wwwroot (ej. /uploads/productos/abc.jpg).</summary>
        public string Ruta { get; set; } = string.Empty;

        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public Producto? Producto { get; set; }
    }
}
