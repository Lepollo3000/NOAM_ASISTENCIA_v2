using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _usermanager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> usermanager)
        {
            _context = context;
            _usermanager = usermanager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] SearchParameters? searchParameters)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            IQueryable<ApplicationUser> originalQuery = _usermanager.Users;

            if (searchParameters == null)
            {
                return Ok(await originalQuery.ToListAsync());
            }
            else
            {
                originalQuery = originalQuery.Where(user => user.UserName != HttpContext.User.Identity!.Name);
                originalQuery = Search(originalQuery, null!);
                originalQuery = Sort(originalQuery, searchParameters.OrderBy!);

                IQueryable<UserDTO> responseQuery = originalQuery
                    .Include(user => user.IdTurnoNavigation)
                    .Select(user => new UserDTO
                    {
                        Id = user.Id,
                        Username = user.UserName,
                        Nombre = user.Nombre,
                        Apellido = user.Apellido,
                        IdTurno = user.IdTurno,
                        NombreTurno = user.IdTurnoNavigation.Descripcion,
                        Lockout = user.Lockout
                    });

                var response = PagedList<UserDTO>.ToPagedList(await responseQuery.ToListAsync(), searchParameters.PageNumber, searchParameters.PageSize);

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(response.MetaData));

                return Ok(response);
            }
        }

        // GET: api/Turnos/5
        [HttpGet("{name}")]
        public async Task<IActionResult> GetUser(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return NotFound();
            }

            ApplicationUser? user = await _usermanager.Users
                .Include(u => u.IdTurnoNavigation)
                .SingleOrDefaultAsync(u => u.UserName == name);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserDTO
            {
                Id = user.Id,
                Username = user.UserName,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                IdTurno = user.IdTurno,
                NombreTurno = user.IdTurnoNavigation.Descripcion,
                Lockout = user.Lockout
            });
        }

        // PUT: api/Turnos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{name}")]
        public async Task<IActionResult> PutUser(string name, UserDTO userDTO)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest();
            }

            ApplicationUser? user = await _usermanager.FindByNameAsync(name);

            if (user == null || name != user.UserName)
            {
                return BadRequest();
            }

            user.Nombre = userDTO.Nombre;
            user.Apellido = userDTO.Apellido;
            user.IdTurno = userDTO.IdTurno;
            user.Lockout = userDTO.Lockout;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(name))
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

        [HttpGet("[action]")]
        public async Task<IActionResult> ForgotPassword()
        {
            return Ok();
        }

        private IQueryable<ApplicationUser> Search(IQueryable<ApplicationUser> users, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return users;

            return null!;
        }

        private IQueryable<ApplicationUser> Sort(IQueryable<ApplicationUser> users, string orderByString)
        {
            if (string.IsNullOrEmpty(orderByString))
                return users.OrderBy(s => s.Id);

            string[] splittedString = orderByString.Split(' ', 2);
            string orderBy = splittedString[0];
            string orderDirection = splittedString[1];

            if (string.IsNullOrEmpty(orderBy) || string.IsNullOrEmpty(orderDirection))
                return users.OrderBy(s => s.Id);

            return orderBy switch
            {
                "username" when orderDirection == "ascending"
                    => users.OrderBy(s => s.UserName),
                "username" when orderDirection == "descending"
                    => users.OrderByDescending(s => s.UserName),

                "nombre" when orderDirection == "ascending"
                    => users.OrderBy(s => s.Nombre),
                "nombre" when orderDirection == "descending"
                    => users.OrderByDescending(s => s.Nombre),

                "apellido" when orderDirection == "ascending"
                    => users.OrderBy(s => s.Apellido),
                "apellido" when orderDirection == "descending"
                    => users.OrderByDescending(s => s.Apellido),

                _ => users.OrderBy(s => s.Id),
            };
        }

        private bool UserExists(string name)
        {
            return (_context.Users?.Any(e => e.UserName == name)).GetValueOrDefault();
        }
    }
}
