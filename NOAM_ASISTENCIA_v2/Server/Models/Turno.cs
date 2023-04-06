using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NOAM_ASISTENCIA_V2.Server.Models
{
    [Table("Turno")]
    public partial class Turno
    {
        public Turno()
        {
            ApplicationUsers = new HashSet<ApplicationUser>();
        }

        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string Descripcion { get; set; } = null!;
        public bool Habilitado { get; set; }

        [InverseProperty("IdTurnoNavigation")]
        public virtual ICollection<ApplicationUser> ApplicationUsers { get; set; }
    }
}
