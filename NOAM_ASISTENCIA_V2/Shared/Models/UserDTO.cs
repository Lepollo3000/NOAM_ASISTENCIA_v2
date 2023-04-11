using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class UserDTO
    {
        [Key]
        public Guid Id { get; set; }
        [Display(Name = "Nombre de Usuario")]
        public string Username { get; set; } = null!;
        [Display(Name = "Nombre(s)")]
        public string Nombre { get; set; } = null!;
        [Display(Name = "Apellido(s)")]
        public string Apellido { get; set; } = null!;
        public int IdTurno { get; set; }
        [Display(Name = "Turno")]
        public string NombreTurno { get; set; } = null!;
        [Display(Name = "Estado")]
        public bool Lockout { get; set; }
    }
}
