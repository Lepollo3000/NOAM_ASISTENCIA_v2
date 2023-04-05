using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NOAM_ASISTENCIA_V2.Server.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public ApplicationUser()
        {
            Asistencias = new HashSet<Asistencia>();
        }

        [Required]
        public string Nombre { get; set; } = null!;
        [Required]
        public string Apellido { get; set; } = null!;
        [Required]
        public int IdTurno { get; set; }
        [Required]
        public bool Lockout { get; set; }
        [Required]
        public bool ForgotPassword { get; set; }

        [ForeignKey("IdTurno")]
        [InverseProperty("ApplicationUsers")]
        public virtual Turno IdTurnoNavigation { get; set; } = null!;
        [InverseProperty("IdUsuarioNavigation")]
        public virtual ICollection<Asistencia> Asistencias { get; set; }
    }
}