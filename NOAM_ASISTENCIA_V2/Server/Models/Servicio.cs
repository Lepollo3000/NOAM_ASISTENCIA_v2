using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NOAM_ASISTENCIA_V2.Server.Models
{
    [Table("Servicio")]
    public partial class Servicio
    {
        public Servicio()
        {
            Asistencia = new HashSet<Asistencia>();
        }

        [Key]
        public int Id { get; set; }
        [StringLength(5)]
        public string CodigoId { get; set; } = null!;
        [StringLength(100)]
        public string Descripcion { get; set; } = null!;
        public bool Habilitado { get; set; }

        [InverseProperty("IdSucursalNavigation")]
        public virtual ICollection<Asistencia> Asistencia { get; set; }
    }
}
