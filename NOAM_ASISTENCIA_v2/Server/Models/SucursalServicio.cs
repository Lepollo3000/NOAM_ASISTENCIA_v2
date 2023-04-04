using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NOAM_ASISTENCIA_v2.Server.Models
{
    [Table("SucursalServicio")]
    public partial class SucursalServicio
    {
        public SucursalServicio()
        {
            Asistencia = new HashSet<Asistencia>();
        }

        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string Descripcion { get; set; } = null!;
        public bool Habilitado { get; set; }

        [InverseProperty("IdSucursalNavigation")]
        public virtual ICollection<Asistencia> Asistencia { get; set; }
    }
}
