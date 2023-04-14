using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOAM_ASISTENCIA_V2.Server.Data;
using NOAM_ASISTENCIA_V2.Server.Models;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;
using System.Linq.Dynamic.Core;

namespace NOAM_ASISTENCIA_V2.Server.Controllers.Intendente
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Intendente, Gerente")]
    public class AsistenciasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AsistenciasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Asistencias
        [HttpGet]
        public async Task<IActionResult> GetAsistencias([FromQuery] SearchParameters searchParameters)
        {
            if (_context.Asistencias == null)
            {
                return NotFound();
            }

            IQueryable<Asistencia> originalQuery = _context.Asistencias
                .Include(a => a.IdUsuarioNavigation)
                .Include(a => a.IdSucursalNavigation);

            originalQuery = Search(originalQuery, null!);
            originalQuery = Sort(originalQuery, searchParameters.OrderBy!);

            return Ok(await _context.Asistencias.ToListAsync());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ReportesAsistencia()
        {
            return NoContent();
        }

        // GET: api/Asistencias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asistencia>> GetAsistencia(Guid id)
        {
            if (_context.Asistencias == null)
            {
                return NotFound();
            }
            var asistencia = await _context.Asistencias.FindAsync(id);

            if (asistencia == null)
            {
                return NotFound();
            }

            return asistencia;
        }

        /*// PUT: api/Asistencias/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsistencia(Guid id, Asistencia asistencia)
        {
            if (id != asistencia.IdUsuario)
            {
                return BadRequest();
            }

            _context.Entry(asistencia).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AsistenciaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }*/

        // POST: api/Asistencias
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostAsistencia(AsistenciaRegistroDTO registro)
        {
            if (_context.Asistencias == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Asistencias' is null.");
            }

            ApplicationUser? user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return BadRequest("El usuario con el que intenta realizar esta acción no se encontró o no existe. Intente de nuevo más tarde o contacte a un administrador.");
            }

            Servicio? servicio = await _context.Servicios.FindAsync(registro.ServicioId);
            if (servicio == null)
            {
                return BadRequest("El servicio con en el que trata de registrarse no se encontró o no existe. Intente de nuevo más tarde o contacte a un administrador.");
            }

            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(registro.TimeZoneId);
            }
            catch (Exception)
            {
                return BadRequest("Error interno del servidor. Intente de nuevo más tarde o contacte a un administrador.");
            }

            // ** TODO A PARTIR DE AQUI DEBE SER MANEJADO CON RESPECTO AL UTC, MUCHO CUIDADO **

            // SE BUSCA UN REGISTRO RECIENTE DE HOY CON FECHA DE SALIDA NULA
            IEnumerable<Asistencia> asistencias = await _context.Asistencias
                .Where(a => a.IdUsuario == user.Id)
                .Where(a => a.IdSucursal == servicio.Id)
                .OrderByDescending(a => a.FechaEntrada)
                .ToListAsync();

            bool esEntrada;
            DateTime fechaActual = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            Asistencia? asistencia = asistencias
                .Where(a => TimeZoneInfo.ConvertTimeFromUtc(a.FechaEntrada, timeZone).Date == fechaActual.Date)
                .Where(a => a.FechaSalida == null)
                .FirstOrDefault();

            /*Asistencia? asistencia = await _context.Asistencias
                .Where(a => a.FechaEntrada.Date.Add(timeZone.BaseUtcOffset) ==
                    DateTime.UtcNow.Date.Add(timeZone.BaseUtcOffset))
                .Where(a => a.FechaSalida == null)
                .FirstOrDefaultAsync();*/

            // SI NO HAY REGISTRO CON LO ANTERIORMENTE MENCIONADO, SE CREA
            if (asistencia == null)
            {
                asistencia = new()
                {
                    IdUsuario = user.Id,
                    IdSucursal = servicio.Id,
                    FechaEntrada = DateTime.UtcNow
                };

                _context.Asistencias.Add(asistencia);

                esEntrada = true;
            }
            else
            {
                asistencia.FechaSalida = DateTime.UtcNow;

                _context.Entry(asistencia).State = EntityState.Modified;

                esEntrada = false;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Error interno del servidor. Intente de nuevo más tarde o contacte a un administrador.");
            }

            return CreatedAtAction("GetAsistencia",
                new { id = asistencia.IdUsuario },
                new AsistenciaRegistroResultDTO
                {
                    Username = user.UserName,
                    Servicio = servicio.Descripcion,
                    Fecha = fechaActual,
                    EsEntrada = esEntrada
                });
        }

        /*// DELETE: api/Asistencias/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsistencia(Guid id)
        {
            if (_context.Asistencias == null)
            {
                return NotFound();
            }
            var asistencia = await _context.Asistencias.FindAsync(id);
            if (asistencia == null)
            {
                return NotFound();
            }

            _context.Asistencias.Remove(asistencia);
            await _context.SaveChangesAsync();

            return NoContent();
        }*/

        private IQueryable<Asistencia> Search(IQueryable<Asistencia> asistencias, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return asistencias;

            return null!;
        }

        private IQueryable<Asistencia> Sort(IQueryable<Asistencia> asistencias, string orderByString)
        {
            if (string.IsNullOrEmpty(orderByString))
                return asistencias.OrderByDescending(s => s.FechaEntrada);

            string[] splittedString = orderByString.Split(' ', 2);
            string orderBy = splittedString[0];
            string orderDirection = splittedString[1];

            if (string.IsNullOrEmpty(orderBy) || string.IsNullOrEmpty(orderDirection))
                return asistencias.OrderByDescending(s => s.FechaEntrada);

            return orderBy switch
            {
                "fechaentrada" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.FechaEntrada),
                "fechaentrada" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.FechaEntrada),

                "username" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.IdUsuarioNavigation.UserName),
                "username" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.IdUsuarioNavigation.UserName),

                "sucursal" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.IdUsuarioNavigation.UserName),
                "sucursal" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.IdUsuarioNavigation.UserName),

                _ => asistencias.OrderByDescending(s => s.FechaEntrada),
            };
        }

        private bool AsistenciaExists(Guid id)
        {
            return (_context.Asistencias?.Any(e => e.IdUsuario == id)).GetValueOrDefault();
        }
    }
}
