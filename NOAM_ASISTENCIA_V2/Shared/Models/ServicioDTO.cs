using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Shared.Models
{
    public class ServicioDTO
    {
        private const string _requiredMessage = "Campo requerido";

        [Required(ErrorMessage = _requiredMessage)]
        public int Id { get; set; }
        [Required(ErrorMessage = _requiredMessage)]
        [Range(0, 99999, ErrorMessage = "Este campo debe ser un NÚMERO de máximo 5 dígitos")]
        [Display(Name = "Número de Identificación")]
        public string CodigoId { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [StringLength(100)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = null!;
        [Required(ErrorMessage = _requiredMessage)]
        [Display(Name = "Estado")]
        public bool Habilitado { get; set; }
    }
}
