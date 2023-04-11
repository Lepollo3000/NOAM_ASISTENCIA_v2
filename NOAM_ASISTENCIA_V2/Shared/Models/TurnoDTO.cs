using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class TurnoDTO
    {
        private const string _requiredMessage = "Campo requerido";

        [Required(ErrorMessage = _requiredMessage)]
        public int Id { get; set; }
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Estado")]
        public bool Habilitado { get; set; }
    }
}
