using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NOAM_ASISTENCIA_V2.Server.Data;
using NOAM_ASISTENCIA_V2.Server.Models;
using NOAM_ASISTENCIA_V2.Server.Utils.Paging;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;

namespace NOAM_ASISTENCIA_V2.Server.Controllers.Administrador
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class TurnosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TurnosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Turnos
        [HttpGet]
        public async Task<IActionResult> GetTurnos([FromQuery] SearchParameters? searchParameters)
        {
            if (_context.Turnos == null)
            {
                return NotFound();
            }

            IQueryable<Turno> originalQuery = _context.Turnos;

            if (searchParameters == null)
            {
                return Ok(originalQuery.ToList());
            }
            else
            {
                originalQuery = originalQuery.Where(t => t.Id != 1);
                originalQuery = Search(originalQuery, null!);
                originalQuery = Sort(originalQuery, searchParameters.OrderBy!);

                IQueryable<TurnoDTO> responseQuery = originalQuery
                    .Select(turno => new TurnoDTO
                    {
                        Id = turno.Id,
                        Descripcion = turno.Descripcion,
                        Habilitado = turno.Habilitado
                    });

                var response = PagedList<TurnoDTO>.ToPagedList(await responseQuery.ToListAsync(), searchParameters.PageNumber, searchParameters.PageSize);

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(response.MetaData));

                return Ok(response);
            }
        }

        // GET: api/Turnos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTurno(int id)
        {
            if (_context.Turnos == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos.FindAsync(id);

            if (turno == null)
            {
                return NotFound();
            }

            return Ok(new TurnoDTO
            {
                Id = turno.Id,
                Descripcion = turno.Descripcion,
                Habilitado = turno.Habilitado
            });
        }

        // PUT: api/Turnos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTurno(int id, TurnoDTO turnoDTO)
        {
            Turno? turno = await _context.Turnos.FindAsync(id);

            if (turno == null || id != turno.Id)
            {
                return BadRequest();
            }

            turno.Descripcion = turnoDTO.Descripcion;
            turno.Habilitado = turnoDTO.Habilitado;

            _context.Entry(turno).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TurnoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return StatusCode(500, "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador");
                }
            }

            return NoContent();
        }

        // POST: api/Turnos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Turno>> PostTurno(Turno turno)
        {
            if (_context.Turnos == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Turnos'  is null.");
            }

            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTurno", new { id = turno.Id }, turno);
        }

        /*// DELETE: api/Turnos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTurno(int id)
        {
            if (_context.Turnos == null)
            {
                return NotFound();
            }
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null)
            {
                return NotFound();
            }

            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();

            return NoContent();
        }*/

        private IQueryable<Turno> Search(IQueryable<Turno> servicios, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return servicios;

            return null!;
        }

        private IQueryable<Turno> Sort(IQueryable<Turno> servicios, string orderByString)
        {
            if (string.IsNullOrEmpty(orderByString))
                return servicios.OrderBy(s => s.Id);

            string[] splittedString = orderByString.Split(' ', 2);
            string orderBy = splittedString[0];
            string orderDirection = splittedString[1];

            if (string.IsNullOrEmpty(orderBy) || string.IsNullOrEmpty(orderDirection))
                return servicios.OrderBy(s => s.Id);

            return orderBy switch
            {
                "descripcion" when orderDirection == "ascending"
                    => servicios.OrderBy(s => s.Descripcion),
                "descripcion" when orderDirection == "descending"
                    => servicios.OrderByDescending(s => s.Descripcion),
                _ => servicios.OrderBy(s => s.Id),
            };
        }

        private bool TurnoExists(int id)
        {
            return (_context.Turnos?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
