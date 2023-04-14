using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models;

public class AsistenciaRegistroResultDTO
{
    [Display(Name = "Usuario")]
    public string Username { get; set; } = null!;
    [Display(Name = "Servicio")]
    public string Servicio { get; set; } = null!;
    public bool EsEntrada { get; set; }
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; }
}
