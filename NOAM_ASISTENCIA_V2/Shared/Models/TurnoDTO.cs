using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class TurnoDTO
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = null!;
        [Required]
        [Display(Name = "Estado")]
        public bool Habilitado { get; set; }
    }
}
