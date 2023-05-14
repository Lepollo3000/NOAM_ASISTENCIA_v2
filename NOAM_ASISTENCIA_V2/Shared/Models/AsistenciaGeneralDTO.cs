using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models;

public class AsistenciaGeneralDTO
{
    [Display(Name = "Usuario")]
    public string Username { get; set; } = null!;
    [Display(Name = "Nombre(s)")]
    public string UsuarioNombre { get; set; } = null!;
    [Display(Name = "Apellido(s)")]
    public string UsuarioApellido { get; set; } = null!;
    [Display(Name = "Horas Laboradas")]
    public double MinutosLaborados { get; set; }
}
