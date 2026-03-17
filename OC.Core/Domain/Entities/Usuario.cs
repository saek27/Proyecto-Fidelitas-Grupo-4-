using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OC.Core.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty; // En producción esto será un Hash
        public bool Activo { get; set; } = true;

        // (Foreign Keys)
        public int RolId { get; set; }
        public Rol Rol { get; set; } = null!;

        public int SucursalId { get; set; }
        public Sucursal Sucursal { get; set; } = null!;
    }
}