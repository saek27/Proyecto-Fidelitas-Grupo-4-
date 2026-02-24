using System.Text.RegularExpressions;

namespace OC.Web.Helpers
{
    // Cédula: 9 dígitos. Ej: 604240201
    public static class CedulaValidation
    {
        public const string FormatoRegex = @"^\d{9}$";
        public const int LongitudCedula = 9;

        public static bool EsFormatoValido(string? cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return false;
            var normalizada = Normalizar(cedula);
            return Regex.IsMatch(normalizada, FormatoRegex);
        }

        // Quita espacios y guiones, deja solo dígitos
        public static string Normalizar(string? cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return string.Empty;
            return new string(cedula.Where(char.IsDigit).ToArray());
        }
    }
}
