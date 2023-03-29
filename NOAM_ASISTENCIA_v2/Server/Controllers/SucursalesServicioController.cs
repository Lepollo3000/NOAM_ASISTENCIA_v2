using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NOAM_ASISTENCIA_v2.Server.Data;
using NOAM_ASISTENCIA_v2.Server.Models;
using NOAM_ASISTENCIA_v2.Server.Utils.Paging;
using NOAM_ASISTENCIA_v2.Server.Utils.Repository;
using NOAM_ASISTENCIA_v2.Shared.RequestFeatures;

namespace NOAM_ASISTENCIA_v2.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SucursalesServicioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SucursalesServicioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SucursalesServicio
        [HttpGet]
        public async Task<IActionResult> GetSucursalServicios([FromQuery] SearchParameters productParameters)
        {
            IQueryable<SucursalServicio> query = _context.SucursalServicios;

            query = Search(query, null!);
            query = Sort(query, productParameters.OrderBy!);

            var response = PagedList<SucursalServicio>.ToPagedList(await query.ToListAsync(), productParameters.PageNumber, productParameters.PageSize);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(response.MetaData));

            return Ok(response);
        }

        /*// GET: api/SucursalesServicio/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SucursalServicio>> GetSucursalServicio(int id)
        {
          if (_context.SucursalServicios == null)
          {
              return NotFound();
          }
            var sucursalServicio = await _context.SucursalServicios.FindAsync(id);

            if (sucursalServicio == null)
            {
                return NotFound();
            }

            return sucursalServicio;
        }

        // PUT: api/SucursalesServicio/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSucursalServicio(int id, SucursalServicio sucursalServicio)
        {
            if (id != sucursalServicio.Id)
            {
                return BadRequest();
            }

            _context.Entry(sucursalServicio).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SucursalServicioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/SucursalesServicio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SucursalServicio>> PostSucursalServicio(SucursalServicio sucursalServicio)
        {
          if (_context.SucursalServicios == null)
          {
              return Problem("Entity set 'ApplicationDbContext.SucursalServicios'  is null.");
          }
            _context.SucursalServicios.Add(sucursalServicio);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSucursalServicio", new { id = sucursalServicio.Id }, sucursalServicio);
        }

        // DELETE: api/SucursalesServicio/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSucursalServicio(int id)
        {
            if (_context.SucursalServicios == null)
            {
                return NotFound();
            }
            var sucursalServicio = await _context.SucursalServicios.FindAsync(id);
            if (sucursalServicio == null)
            {
                return NotFound();
            }

            _context.SucursalServicios.Remove(sucursalServicio);
            await _context.SaveChangesAsync();

            return NoContent();
        }*/

        private IQueryable<SucursalServicio> Search(IQueryable<SucursalServicio> servicios, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return servicios;

            return null!;
        }

        private IQueryable<SucursalServicio> Sort(IQueryable<SucursalServicio> servicios, string orderByString)
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

        private bool SucursalServicioExists(int id)
        {
            return (_context.SucursalServicios?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
