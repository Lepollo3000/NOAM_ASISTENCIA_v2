using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class UserDTO
    {
        private const string _requiredMessage = "Campo requerido";

        [Required(ErrorMessage = _requiredMessage)]
        public Guid Id { get; set; }
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
        [Display(Name = "Olvidó su contraseña")]
        public bool ForgotPassword { get; set; }
    }
}
