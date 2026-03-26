using System;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    // Estados de cita
    public static class EstadoCita
    {
        public const string Pendiente = "Pendiente";
        public const string Confirmada = "Confirmada";
        public const string Cancelada = "Cancelada";
        public const string Atendida = "Atendida";
    }

    public class Asistencia
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime? HoraEntrada { get; set; }

        public DateTime? HoraSalida { get; set; }

        public bool Activo { get; set; } = true;
    }
}
