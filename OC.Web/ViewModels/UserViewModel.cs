using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; } // Agregamos el ID para saber a quién editamos


        // Campos de entrada
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre Completo")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // QUITAMOS [Required] de aquí. Lo validaremos en el Controller.
        [DataType(DataType.Password)]
        public string? Password { get; set; } // Hacemos nullable

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "Debe seleccionar un Rol")]
        public int RoleId { get; set; }

        [Display(Name = "Sucursal")]
        [Required(ErrorMessage = "Debe seleccionar una Sucursal")]
        public int SucursalId { get; set; }

        // Listas para los Dropdowns (<select>)
        public IEnumerable<SelectListItem>? RolesList { get; set; }
        public IEnumerable<SelectListItem>? SucursalesList { get; set; }
    }
}