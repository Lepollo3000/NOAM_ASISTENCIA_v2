using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class UserDTO
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = null!;
        [Required]
        [Display(Name = "Nombre(s)")]
        public string Nombre { get; set; } = null!;
        [Required]
        [Display(Name = "Apellido(s)")]
        public string Apellido { get; set; } = null!;
        [Required]
        [Display(Name = "Turno")]
        public int IdTurno { get; set; }
        [Display(Name = "Turno")]
        public string? NombreTurno { get; set; } = null!;
        [Required]
        [Display(Name = "Estado")]
        public bool Lockout { get; set; }
    }
}
