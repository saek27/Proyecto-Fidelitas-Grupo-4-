using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.Core.Domain.Entities
{
    public class Empleado
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;

        public int SucursalId { get; set; }
        public bool Activo { get; set; }

        // Relación
        public Sucursal Sucursal { get; set; } = null!;
    }
}

