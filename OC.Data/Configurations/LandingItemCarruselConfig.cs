using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class LandingItemCarruselConfig : IEntityTypeConfiguration<LandingItemCarrusel>
    {
        public void Configure(EntityTypeBuilder<LandingItemCarrusel> builder)
        {
            builder.ToTable("CarruselItems");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Posicion)
                .IsRequired();

            builder.Property(c => c.Tipo)
                .IsRequired()
                .HasMaxLength(20);

            // FK Producto: nullable, cascade si se borra el Producto
            builder.HasOne(c => c.Producto)
                .WithMany()
                .HasForeignKey(c => c.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK Aro: nullable, cascade si se borra el Aro
            builder.HasOne(c => c.Aro)
                .WithMany()
                .HasForeignKey(c => c.AroId)
                .OnDelete(DeleteBehavior.Cascade);

            // Posicion es única (slots fijos)
            builder.HasIndex(c => c.Posicion)
                .IsUnique()
                .HasDatabaseName("UX_CarruselItems_Posicion");
        }
    }
}