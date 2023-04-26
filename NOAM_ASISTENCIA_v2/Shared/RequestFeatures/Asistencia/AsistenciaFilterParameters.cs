using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.RequestFeatures.Asistencia;

public class AsistenciaFilterParameters
{
    public string? Username { get; set; } = null!;
    public string TimeZoneId { get; set; } = null!;
    [Display(Name = "Fecha Inicial")]
    public DateTime? FechaInicial { get; set; }
    [Display(Name = "Fecha Final")]
    public DateTime? FechaFinal { get; set; }
}
