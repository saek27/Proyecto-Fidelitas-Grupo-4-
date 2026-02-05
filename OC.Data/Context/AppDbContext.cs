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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}