using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Aro
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool Activo { get; set; } = true;

        [MaxLength(512)]
        public string? RutaImagen { get; set; }

        /// <summary>Si true, el aro aparece en el catálogo público del landing (sección "Lentes Graduados").</summary>
        public bool MostrarEnLanding { get; set; } = false;

        /// <summary>Resumen breve del aro que se muestra en el landing (carrusel y destacados). Opcional, máx 500 chars.</summary>
        [MaxLength(500)]
        public string? DescripcionCorta { get; set; }

        /// <summary>Imágenes múltiples del aro (carrusel del catálogo).</summary>
        public ICollection<AroImagen> Imagenes { get; set; } = new List<AroImagen>();
    }
}