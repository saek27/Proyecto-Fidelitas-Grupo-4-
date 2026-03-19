using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OC.Core.Domain.Entities;

namespace OC.Data.Context
{
    public static class DbInitializer
    {
        /// <summary>Crea la tabla OrdenesTrabajo si no existe (respaldo cuando la migración no se aplica correctamente).</summary>
        public static void EnsureOrdenesTrabajoTable(AppDbContext context)
        {
            const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrdenesTrabajo')
BEGIN
    CREATE TABLE [OrdenesTrabajo] (
        [Id] int NOT NULL IDENTITY(1,1),
        [PacienteId] int NOT NULL,
        [SucursalId] int NOT NULL,
        [VentaId] int NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Referencia] nvarchar(200) NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaLista] datetime2 NULL,
        CONSTRAINT [PK_OrdenesTrabajo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrdenesTrabajo_Pacientes_PacienteId] FOREIGN KEY ([PacienteId]) REFERENCES [Pacientes]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrdenesTrabajo_Sucursales_SucursalId] FOREIGN KEY ([SucursalId]) REFERENCES [Sucursales]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrdenesTrabajo_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_OrdenesTrabajo_PacienteId] ON [OrdenesTrabajo]([PacienteId]);
    CREATE INDEX [IX_OrdenesTrabajo_SucursalId] ON [OrdenesTrabajo]([SucursalId]);
    CREATE INDEX [IX_OrdenesTrabajo_VentaId] ON [OrdenesTrabajo]([VentaId]);
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>Crea/ajusta la tabla EnviosNotificacion para que OT-HU-023 pueda registrar notificaciones.</summary>
        public static void EnsureEnviosNotificacionTable(AppDbContext context)
        {
            // Nota: evitamos depender de migraciones que alteren/crean esta tabla.
            // Ajustamos mínimamente para que Inserte con CitaId/OrdenTrabajoId NULL permitidos.
            var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EnviosNotificacion')
BEGIN
    CREATE TABLE [EnviosNotificacion] (
        [Id] int NOT NULL IDENTITY(1,1),
        [CitaId] int NULL,
        [OrdenTrabajoId] int NULL,
        [TipoNotificacion] nvarchar(50) NOT NULL,
        [FechaHoraEnvio] datetime2 NOT NULL,
        [Canal] nvarchar(20) NOT NULL DEFAULT ('Email'),
        [Destinatario] nvarchar(256) NULL,
        [MensajeResumen] nvarchar(500) NULL,
        [Exito] bit NOT NULL DEFAULT (1),
        CONSTRAINT [PK_EnviosNotificacion] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_EnviosNotificacion_CitaId] ON [EnviosNotificacion] ([CitaId]);
    CREATE INDEX [IX_EnviosNotificacion_OrdenTrabajoId] ON [EnviosNotificacion] ([OrdenTrabajoId]);
END
ELSE
BEGIN
    -- Si falta OrdenTrabajoId (p. ej. por migraciones cortadas), lo agregamos.
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EnviosNotificacion') AND name = 'OrdenTrabajoId')
    BEGIN
        ALTER TABLE [EnviosNotificacion] ADD [OrdenTrabajoId] int NULL;
        CREATE INDEX [IX_EnviosNotificacion_OrdenTrabajoId] ON [EnviosNotificacion] ([OrdenTrabajoId]);
    END

    -- OT-HU-023 inserta registros con CitaId = NULL; CitaId debe ser nullable.
    IF EXISTS (
        SELECT * FROM sys.columns
        WHERE object_id = OBJECT_ID('EnviosNotificacion') AND name = 'CitaId' AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [EnviosNotificacion] ALTER COLUMN [CitaId] int NULL;
        -- No es necesario recrear índices; SQL Server los conserva.
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        /// <summary>Crea columnas de notificación en Citas si faltan en la BD.</summary>
        public static void EnsureCitasNotificationColumns(AppDbContext context)
        {
            var sql = @"
IF OBJECT_ID('Citas','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Citas') AND name = 'NotificacionesActivas'
    )
    BEGIN
        ALTER TABLE Citas ADD NotificacionesActivas bit NOT NULL CONSTRAINT DF_Citas_NotificacionesActivas DEFAULT (1);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Citas') AND name = 'CanalNotificacion'
    )
    BEGIN
        ALTER TABLE Citas ADD CanalNotificacion nvarchar(20) NOT NULL CONSTRAINT DF_Citas_CanalNotificacion DEFAULT ('Email');
    END
END
";
            context.Database.ExecuteSqlRaw(sql);
        }

        public static void Initialize(AppDbContext context)
        {
            // 1. Solo crear sucursal si no hay ninguna
            if (!context.Sucursales.Any())
            {
                context.Sucursales.Add(new Sucursal
                {
                    Nombre = "Sucursal Matriz",
                    Direccion = "...",
                    Telefono = "...",
                    Activo = true // Importante: UI de Órdenes filtra por Activo
                });
                context.SaveChanges();
            }

            // 2. Solo crear roles si la tabla está vacía
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Rol { Nombre = "Admin", Descripcion = "Acceso Total" },
                    new Rol { Nombre = "Optometrista", Descripcion = "Personal Médico" },
                    new Rol { Nombre = "Recepcion", Descripcion = "Atención al Cliente" },
                    new Rol { Nombre = "Paciente", Descripcion = "Paciente del Sistema" },
                    new Rol { Nombre = "Tecnico", Descripcion = "Técnico de soporte" }
                );
                context.SaveChanges();
            }
            else
            {
                if (!context.Roles.Any(r => r.Nombre == "Paciente"))
                {
                    context.Roles.Add(new Rol { Nombre = "Paciente", Descripcion = "Paciente del Sistema" });
                    context.SaveChanges();
                }
                if (!context.Roles.Any(r => r.Nombre == "Tecnico"))
                {
                    context.Roles.Add(new Rol { Nombre = "Tecnico", Descripcion = "Técnico de soporte" });
                    context.SaveChanges();
                }
            }

            // 3. Solo crear el admin si no existe el correo
            if (!context.Usuarios.Any(u => u.Correo == "admin@optica.com"))
            {
                var rolAdmin = context.Roles.First(r => r.Nombre == "Admin");
                var sucursal = context.Sucursales.First();

                context.Usuarios.Add(new Usuario
                {
                    Nombre = "Administrador",
                    Correo = "admin@optica.com",
                    Contrasena = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Activo = true,
                    RolId = rolAdmin.Id,
                    SucursalId = sucursal.Id
                });
                context.SaveChanges();
            }
        }
    }
}