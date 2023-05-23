using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class IndexAdministradorDTO
    {
        [Display(Name = "Asistencias del Día")]
        public int AsistenciasDelDia { get; set; }
    }
}
