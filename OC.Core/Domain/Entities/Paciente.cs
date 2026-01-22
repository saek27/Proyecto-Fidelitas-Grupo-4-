using System;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    public class Paciente
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty; // O Documento de Identidad
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedad calculada (útil para la UI)
        public string NombreCompleto => $"{Nombres} {Apellidos}";

        // Aqui luego agregaremos la relación con Consultas/Lentes
        // public ICollection<Consulta> Consultas { get; set; }
    }
}