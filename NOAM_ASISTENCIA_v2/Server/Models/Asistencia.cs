using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NOAM_ASISTENCIA_V2.Server.Models
{
    public partial class Asistencia
    {
        [Key]
        public Guid IdUsuario { get; set; }
        [Key]
        public int IdSucursal { get; set; }
        [Key]
        [Column(TypeName = "datetime")]
        public DateTime FechaEntrada { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? FechaSalida { get; set; }

        [ForeignKey("IdUsuario")]
        [InverseProperty("Asistencias")]
        public virtual ApplicationUser IdUsuarioNavigation { get; set; } = null!;

        [ForeignKey("IdSucursal")]
        [InverseProperty("Asistencia")]
        public virtual Servicio IdSucursalNavigation { get; set; } = null!;
    }
}
