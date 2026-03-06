using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class TicketCierreViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar observaciones de cierre")]
        public string Observaciones { get; set; } = string.Empty;
    }
}