using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class AroImagenConfig : IEntityTypeConfiguration<AroImagen>
    {
        public void Configure(EntityTypeBuilder<AroImagen> builder)
        {
            builder.ToTable("AroImagenes");

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

            builder.HasOne(i => i.Aro)
                .WithMany(a => a.Imagenes)
                .HasForeignKey(i => i.AroId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(i => i.AroId)
                .HasDatabaseName("IX_AroImagenes_AroId");

            builder.HasIndex(i => new { i.AroId, i.Activo })
                .HasDatabaseName("IX_AroImagenes_AroId_Activo");
        }
    }
}
