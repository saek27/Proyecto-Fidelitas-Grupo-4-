using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class LandingItemDestacadoConfig : IEntityTypeConfiguration<LandingItemDestacado>
    {
        public void Configure(EntityTypeBuilder<LandingItemDestacado> builder)
        {
            builder.ToTable("DestacadosItems");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Posicion)
                .IsRequired();

            builder.Property(d => d.Tipo)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Aro)
                .WithMany()
                .HasForeignKey(d => d.AroId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(d => d.Posicion)
                .IsUnique()
                .HasDatabaseName("UX_DestacadosItems_Posicion");
        }
    }
}