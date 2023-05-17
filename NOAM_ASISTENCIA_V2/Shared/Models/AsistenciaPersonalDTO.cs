using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models;

public class AsistenciaPersonalDTO
{
    [Display(Name = "Usuario")]
    public string Username { get; set; } = null!;
    [Display(Name = "Nombre(s)")]
    public string NombreUsuario { get; set; } = null!;
    [Display(Name = "Apellido(s)")]
    public string ApellidoUsuario { get; set; } = null!;
    [Display(Name = "Servicio")]
    public string NombreServicio { get; set; } = null!;
    [Display(Name = "Fecha")]
    public DateTime FechaEntrada { get; set; }
    public DateTime? FechaSalida { get; set; }
    [Display(Name = "Horas Laboradas")]
    public double MinutosLaborados { get; set; }
}
