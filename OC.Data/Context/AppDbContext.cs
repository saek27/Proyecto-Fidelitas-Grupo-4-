using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Domain.Entities;
using System.Reflection;


namespace OC.Data.Context
{
    public class AppDbContext : DbContext
    {
        // OJO AQUÍ: Debe decir <AppDbContext> dentro de los símbolos menor/mayor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet <Empleado> Empleados { get; set; }
        public DbSet<SolicitudCita> SolicitudesCitas { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<Expediente> Expedientes { get; set; }
        public DbSet<ValorClinico> ValoresClinicos { get; set; }
        public DbSet<DocumentoExpediente> DocumentosExpediente { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.Entity<ValorClinico>(entity =>
            {
                entity.Property(e => e.EsferaOD).HasPrecision(4, 2);
                entity.Property(e => e.CilindroOD).HasPrecision(4, 2);
                entity.Property(e => e.EjeOD).HasPrecision(4, 2);
                entity.Property(e => e.EsferaOI).HasPrecision(4, 2);
                entity.Property(e => e.CilindroOI).HasPrecision(4, 2);
                entity.Property(e => e.EjeOI).HasPrecision(4, 2);
            });

            modelBuilder.Entity<Expediente>()
            .HasOne(e => e.Cita)
            .WithOne(c => c.Expediente)
            .HasForeignKey<Expediente>(e => e.CitaId)
            .OnDelete(DeleteBehavior.Restrict); // o Cascade según necesidad

        }
    }
}