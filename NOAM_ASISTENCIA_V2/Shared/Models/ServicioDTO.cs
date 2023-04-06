using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class ServicioDTO
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [Range(0, 99999, ErrorMessage = "Este campo debe ser un NÚMERO de máximo 5 dígitos")]
        [Display(Name = "Número de Identificación")]
        public string CodigoId { get; set; } = null!;
        [Required]
        [StringLength(100)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = null!;
        [Display(Name = "Estado")]
        public bool Habilitado { get; set; }
    }
}
