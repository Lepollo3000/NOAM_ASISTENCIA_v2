using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models;

public class AsistenciaRegistroDTO
{
    [Display(Name = "Sucursal")]
    public int ServicioId { get; set; }
    [Display(Name = "Zona Horaria")]
    public string TimeZoneId { get; set; } = null!;
}
