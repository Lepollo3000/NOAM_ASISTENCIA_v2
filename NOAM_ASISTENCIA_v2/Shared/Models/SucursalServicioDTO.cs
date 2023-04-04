using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_v2.Shared.Models
{
    public class SucursalServicioDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = null!;
        [Display(Name = "Habilitado")]
        public bool Habilitado { get; set; }
    }
}
