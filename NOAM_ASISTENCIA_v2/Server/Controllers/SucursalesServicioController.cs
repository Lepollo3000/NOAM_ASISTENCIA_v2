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
        public async Task<IActionResult> GetSucursalServicios([FromQuery] ProductParameters productParameters)
        {
            IEnumerable<SucursalServicio> registries = await _context.SucursalServicios
                  .Search(productParameters.SearchTerm!)
                  .Sort(productParameters.OrderBy!)
                  .ToListAsync();

            var response = PagedList<SucursalServicio>.ToPagedList(registries, productParameters.PageNumber, productParameters.PageSize);

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

        private bool SucursalServicioExists(int id)
        {
            return (_context.SucursalServicios?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
