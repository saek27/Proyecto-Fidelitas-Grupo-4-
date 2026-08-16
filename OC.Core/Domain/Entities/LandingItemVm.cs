namespace OC.Core.Domain.Entities
{
    /// <summary>DTO mínimo para mezclar Productos y Aros en el Index del landing.</summary>
    public class LandingItemVm
    {
        public string Tipo { get; set; } = ""; // "Producto" | "Aro"
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string? RutaImagenLegacy { get; set; }
        public string DetalleUrl { get; set; } = "";
    }
}
