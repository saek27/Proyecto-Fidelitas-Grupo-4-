using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OC.Core.Domain.Entities;
using System.Linq;

namespace OC.Data.Context
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // 1. Solo crear sucursal si no hay ninguna
            if (!context.Sucursales.Any())
            {
                context.Sucursales.Add(new Sucursal { Nombre = "Sucursal Matriz", Direccion = "...", Telefono = "..." });
                context.SaveChanges();
            }

            // 2. Solo crear roles si la tabla está vacía
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Rol { Nombre = "Admin", Descripcion = "Acceso Total" },
                    new Rol { Nombre = "Optometrista", Descripcion = "Personal Médico" },
                    new Rol { Nombre = "Recepcion", Descripcion = "Atención al Cliente" },
                    new Rol { Nombre = "Paciente", Descripcion = "Paciente del Sistema" }
                );
                context.SaveChanges();
            }
            else
            {
                // Si los roles ya existen, verificar si existe el rol Paciente y agregarlo si no existe
                if (!context.Roles.Any(r => r.Nombre == "Paciente"))
                {
                    context.Roles.Add(new Rol { Nombre = "Paciente", Descripcion = "Paciente del Sistema" });
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