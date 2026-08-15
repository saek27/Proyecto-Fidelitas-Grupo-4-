using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class ProductoImagenConfig : IEntityTypeConfiguration<ProductoImagen>
    {
        public void Configure(EntityTypeBuilder<ProductoImagen> builder)
        {
            builder.ToTable("ProductoImagenes");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Ruta)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(i => i.Orden)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(i => i.EsPrincipal)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(i => i.Activo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(i => i.FechaCreacion)
                .IsRequired();

            // FK con cascade: si se borra el Producto, se borra la fila en ProductoImagenes
            builder.HasOne(i => i.Producto)
                .WithMany(p => p.Imagenes)
                .HasForeignKey(i => i.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice principal por producto
            builder.HasIndex(i => i.ProductoId)
                .HasDatabaseName("IX_ProductoImagenes_ProductoId");

            // Índice filtrado: solo activas (consulta más rápida en el landing)
            builder.HasIndex(i => new { i.ProductoId, i.Activo })
                .HasDatabaseName("IX_ProductoImagenes_ProductoId_Activo");
        }
    }
}
