using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    public class Sucursal
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? Telefono { get; set; }

        // Relaciones
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        // Aquí luego pondremos el Inventario
    }
}