using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NOAM_ASISTENCIA_V2.Server.Models;

namespace NOAM_ASISTENCIA_V2.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public virtual DbSet<Asistencia> Asistencias { get; set; } = null!;
        public virtual DbSet<Servicio> Servicios { get; set; } = null!;
        public virtual DbSet<Turno> Turnos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Asistencia>(entity =>
            {
                entity.HasKey(e => new { e.IdUsuario, e.IdSucursal, e.FechaEntrada });

                entity.HasOne(d => d.IdSucursalNavigation)
                    .WithMany(p => p.Asistencia)
                    .HasForeignKey(d => d.IdSucursal)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Asistencia_SucursalServicio1");

                entity.HasOne(d => d.IdUsuarioNavigation)
                    .WithMany(p => p.Asistencias)
                    .HasForeignKey(d => d.IdUsuario)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Asistencia_Usuario");
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(d => d.IdTurnoNavigation)
                    .WithMany(p => p.ApplicationUsers)
                    .HasForeignKey(d => d.IdTurno)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Usuario_Turno");
            });
        }
    }
}