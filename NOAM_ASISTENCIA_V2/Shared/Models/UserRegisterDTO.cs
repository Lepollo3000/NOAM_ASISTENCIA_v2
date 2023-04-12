using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class UserRegisterDTO
    {
        private const string _requiredMessage = "Campo requerido";

        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Nombre(s)")]
        public string Nombre { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Apellido(s)")]
        public string Apellido { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Turno")]
        public int IdTurno { get; set; }
        [Display(Name = "Turno")]
        public string NombreTurno { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Estado")]
        public bool Lockout { get; set; }
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Contraseña")]
        [RegularExpression("^(?=\\S*[a-z])(?=\\S*[A-Z])(?=\\S*\\d)(?=\\S*[^\\w\\s])\\S{8,}$",
            ErrorMessage = "Debe contener al menos una mayúscula, un caracter especial y un número, además de al menos 6 caracteres.")]
        public string Password { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas deben coincidir.")]
        public string ConfirmPassword { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Roles")]
        public IEnumerable<string> Roles { get; set; } = null!;
    }
}
