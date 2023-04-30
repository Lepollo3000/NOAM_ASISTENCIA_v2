using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NOAM_ASISTENCIA_V2.Server.Data;
using NOAM_ASISTENCIA_V2.Server.Data.Migrations;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> usermanager, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = usermanager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] SearchParameters? searchParameters)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            IQueryable<ApplicationUser> originalQuery = _userManager.Users;

            if (searchParameters == null)
            {
                return Ok(await originalQuery.ToListAsync());
            }
            else
            {
                originalQuery = originalQuery
                    .Where(user => user.UserName != HttpContext.User.Identity!.Name)
                    .Include(user => user.IdTurnoNavigation)
                    .Sort(searchParameters.OrderBy!)
                    .Search(null!);

                // SE ENLISTAN LOS USUARIOS PARA PODER SACAR LOS ROLES DE CADA UNO
                List<ApplicationUser> responseList = await originalQuery.ToListAsync();

                // SE HACE UN BUCLE POR CADA USUARIO PARA OBTENER SUS RESPECTIVOS ROLES
                List<UserDTO> responseQuery = new();
                foreach (ApplicationUser user in responseList)
                {
                    responseQuery.Add(new UserDTO
                    {
                        Username = user.UserName,
                        Nombre = user.Nombre,
                        Apellido = user.Apellido,
                        IdTurno = user.IdTurno,
                        NombreTurno = user.IdTurnoNavigation.Descripcion,
                        Lockout = user.Lockout,
                        ForgotPassword = user.ForgotPassword,
                        Roles = await _userManager.GetRolesAsync(user)
                    });
                }

                var response = PagedList<UserDTO>.ToPagedList(responseQuery, searchParameters.PageNumber,
                    searchParameters.PageSize);

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(response.MetaData));

                return Ok(response);
            }
        }

        // GET: api/Users/5
        [HttpGet("{name}")]
        public async Task<IActionResult> GetUser(string name, bool isEditing)
        {
            if (string.IsNullOrEmpty(name))
            {
                return NotFound();
            }

            ApplicationUser? user = await _userManager.Users
                .Include(u => u.IdTurnoNavigation)
                .SingleOrDefaultAsync(u => u.UserName == name);

            if (user == null)
            {
                return NotFound();
            }

            if (isEditing)
            {
                return Ok(new UserEditDTO
                {
                    Username = user.UserName,
                    Nombre = user.Nombre,
                    Apellido = user.Apellido,
                    IdTurno = user.IdTurno,
                    NombreTurno = user.IdTurnoNavigation.Descripcion,
                    Lockout = user.Lockout,
                    ForgotPassword = user.ForgotPassword,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }
            else
            {

                return Ok(new UserDTO
                {
                    Username = user.UserName,
                    Nombre = user.Nombre,
                    Apellido = user.Apellido,
                    IdTurno = user.IdTurno,
                    NombreTurno = user.IdTurnoNavigation.Descripcion,
                    Lockout = user.Lockout,
                    ForgotPassword = user.ForgotPassword
                });
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetRoles()
        {
            if (_roleManager.Roles == null)
            {
                return NotFound();
            }

            IQueryable<string> response = _roleManager.Roles.Select(r => r.Name);

            return Ok(await response.ToListAsync());
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostUser(UserRegisterDTO registerDTO)
        {
            ApplicationUser? oUser = await _userManager.FindByNameAsync(registerDTO.Username);

            // IF THERE IS ALREADY A USER NAMED LIKE THAT
            if (oUser != null)
            {
                return BadRequest();
            }

            oUser = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                UserName = registerDTO.Username,
                Nombre = registerDTO.Nombre,
                Apellido = registerDTO.Apellido,
                IdTurno = registerDTO.IdTurno
            };

            // CREATE USER
            IdentityResult createResult = await _userManager.CreateAsync(oUser, registerDTO.Password);

            // IF USER CANNOT BE CREATED
            if (!createResult.Succeeded)
            {
                string error = "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador";

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            // CONFIRM EMAIL
            string token = await _userManager.GenerateEmailConfirmationTokenAsync(oUser);
            IdentityResult confirmEmailResult = await _userManager.ConfirmEmailAsync(oUser, token);

            // IF MAIL CANNOT BE CONFIRMED
            if (!confirmEmailResult.Succeeded)
            {
                string error = "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador";

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            IdentityResult rolesAddedResult;

            if (registerDTO.Roles.Any())
            {
                rolesAddedResult = await _userManager.AddToRolesAsync(oUser, registerDTO.Roles);
            }
            else
            {
                rolesAddedResult = await _userManager.AddToRoleAsync(oUser, "Intendente");
            }

            // IF ROLES CANNOT BE ADDED
            if (!rolesAddedResult.Succeeded)
            {
                string error = "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador";

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            return NoContent();
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{name}")]
        public async Task<IActionResult> PutUser(string name, UserEditDTO userDTO)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest();
            }

            ApplicationUser? user = await _userManager.FindByNameAsync(name);

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
                    string error = "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador";

                    return StatusCode(StatusCodes.Status500InternalServerError, error);
                }
            }

            IdentityResult rolesAddedResult;

            if (userDTO.Roles.Any())
            {
                await _userManager.RemoveFromRoleAsync(user, "Intendente");
                await _userManager.RemoveFromRoleAsync(user, "Gerente");
                await _userManager.RemoveFromRoleAsync(user, "Administrador");

                rolesAddedResult = await _userManager.AddToRolesAsync(user, userDTO.Roles);
            }
            else
            {
                await _userManager.RemoveFromRoleAsync(user, "Intendente");
                await _userManager.RemoveFromRoleAsync(user, "Gerente");
                await _userManager.RemoveFromRoleAsync(user, "Administrador");

                rolesAddedResult = await _userManager.AddToRoleAsync(user, "Intendente");
            }

            // IF ROLES CANNOT BE ADDED
            if (!rolesAddedResult.Succeeded)
            {
                string error = "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador";

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            return NoContent();
        }

        [HttpPut("[action]/{name}")]
        public async Task<IActionResult> ForgotPassword(string name, UserPasswordResetDTO passwordChange)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest();
            }

            ApplicationUser? user = await _userManager.FindByNameAsync(name);

            if (user == null || name != user.UserName)
            {
                return BadRequest();
            }

            if (user.ForgotPassword)
            {
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                IdentityResult result = await _userManager.ResetPasswordAsync(user, token, passwordChange.NewPassword);

                if (result.Succeeded)
                {
                    user.ForgotPassword = false;

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
                            string error = "Lo sentimos, ocurrió un error inesperado. Inténtelo de nuevo más tarde o consulte a un administrador";

                            return StatusCode(StatusCodes.Status500InternalServerError, error);
                        }
                    }

                    return NoContent();
                }
            }

            return NoContent();
        }

        private bool UserExists(string name)
        {
            return (_context.Users?.Any(e => e.UserName == name)).GetValueOrDefault();
        }
    }

    public static class UsersExtensions
    {
        public static IQueryable<ApplicationUser> Search(this IQueryable<ApplicationUser> users, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return users;

            return null!;
        }

        public static IQueryable<ApplicationUser> Sort(this IQueryable<ApplicationUser> users, string orderByString)
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
    }

}
