using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models;

public class AsistenciaGeneralDTO
{
    private const string _requiredMessage = "Campo requerido";

    [Required(ErrorMessage = _requiredMessage)]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = null!;
    [Required(ErrorMessage = _requiredMessage)]
    [Display(Name = "Nombre(s)")]
    public string UsuarioNombre { get; set; } = null!;
    [Required(ErrorMessage = _requiredMessage)]
    [Display(Name = "Apellido(s)")]
    public string UsuarioApellido { get; set; } = null!;
    [Required(ErrorMessage = _requiredMessage)]
    [Display(Name = "Horas Laboradas")]
    public double HorasLaboradas { get; set; }
}
