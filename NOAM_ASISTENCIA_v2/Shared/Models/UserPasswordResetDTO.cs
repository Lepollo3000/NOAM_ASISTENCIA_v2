using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class UserPasswordResetDTO
    {
        private const string _requiredMessage = "Campo requerido";

        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Nueva Contraseña")]
        [RegularExpression("^(?=\\S*[a-z])(?=\\S*[A-Z])(?=\\S*\\d)(?=\\S*[^\\w\\s])\\S{8,}$",
            ErrorMessage = "Debe contener al menos una mayúscula, un caracter especial y un número, además de al menos 6 caracteres.")]
        public string NewPassword { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas deben coincidir.")]
        public string ConfirmNewPassword { get; set; } = null!;
    }
}
