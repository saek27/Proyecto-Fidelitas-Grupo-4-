using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OC.Core.Domain.Entities;

namespace OC.Data.Configurations
{
    public class PacienteConfig : IEntityTypeConfiguration<Paciente>
    {
        public void Configure(EntityTypeBuilder<Paciente> builder)
        {
            // Nombre de la tabla
            builder.ToTable("Pacientes");

            // Clave primaria
            builder.HasKey(x => x.Id);

            // Propiedades
            builder.Property(x => x.Nombres)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Apellidos)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Cedula)
                .IsRequired()
                .HasMaxLength(20);

            // Índice único para no repetir pacientes por cédula
            builder.HasIndex(x => x.Cedula)
                .IsUnique();

            builder.Property(x => x.Telefono)
                .HasMaxLength(20);

            builder.Property(x => x.Email)
                .HasMaxLength(100);
        }
    }
}