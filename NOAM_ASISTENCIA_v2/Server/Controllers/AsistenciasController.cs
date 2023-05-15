using IronXL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Extensions;
using Newtonsoft.Json;
using NOAM_ASISTENCIA_V2.Client.Pages.Intendente.Asistencia;
using NOAM_ASISTENCIA_V2.Client.Utils;
using NOAM_ASISTENCIA_V2.Server.Data;
using NOAM_ASISTENCIA_V2.Server.Models;
using NOAM_ASISTENCIA_V2.Server.Utils.Paging;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures.Asistencia;
using System.Linq;
using System.Linq.Dynamic.Core;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NOAM_ASISTENCIA_V2.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Intendente, Gerente")]
    public class AsistenciasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public AsistenciasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: api/Asistencias
        [HttpGet]
        public async Task<IActionResult> GetAsistencias([FromQuery] SearchParameters parameters, [FromQuery] AsistenciaFilterParameters filters, bool esReporteGeneral)
        {
            #region Validaciones
            if (_context.Asistencias == null)
            {
                return NotFound();
            }

            if (filters.TimeZoneId == null)
            {
                return BadRequest("Se requiere la zona horaria.");
            }

            // SE OBTIENE LA ZONA HORARIA PROPORCIONADA
            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(filters.TimeZoneId);
            }
            catch (Exception)
            {
                return BadRequest("La zona horaria proporcionada no se encontró o no existe. Intente de nuevo más tarde o contacte a un administrador.");
            }
            #endregion

            if (esReporteGeneral)
            {
                DateTime fechaInicial = filters.FechaMes.HasValue
                    ? new DateTime(filters.FechaMes.Value.Year, filters.FechaMes.Value.Month, 1)
                    : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                DateTime fechaFinal = new DateTime(fechaInicial.Year, fechaInicial.Month, DateTime
                        .DaysInMonth(fechaInicial.Year, fechaInicial.Month));

                IEnumerable<AsistenciaGeneralDTO> responseList = await GetReportesGenerales(parameters,
                    filters, timeZone, fechaInicial, fechaFinal);

                var response = PagedList<AsistenciaGeneralDTO>.ToPagedList(responseList,
                        parameters.PageNumber, parameters.PageSize);

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(response.MetaData));

                return Ok(response);
            }
            else
            {
                DateTime fechaInicial = filters.FechaMes.HasValue
                    ? filters.FechaMes.Value.Date
                    : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                DateTime fechaFinal = fechaInicial.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                ApplicationUser? user;

                #region Validaciones
                if (filters.Username == null)
                {
                    user = await _userManager.GetUserAsync(HttpContext.User);
                }
                else
                {
                    user = await _userManager.FindByNameAsync(filters.Username);
                }

                if (user == null)
                {
                    return BadRequest("El usuario no se encontró o no existe. Intente de nuevo más tarde o contacte a un administrador.");
                }
                #endregion

                IEnumerable<AsistenciaPersonalDTO> responseList = await GetReportesPersonales(parameters,
                    filters, timeZone, fechaInicial, fechaFinal, user);

                var response = PagedList<AsistenciaPersonalDTO>.ToPagedList(responseList,
                        parameters.PageNumber, parameters.PageSize);

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(response.MetaData));

                return Ok(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ReporteAsistencia([FromQuery] SearchParameters parameters, [FromQuery] AsistenciaFilterParameters filters)
        {
            #region Validaciones
            if (_context.Asistencias == null)
            {
                return NotFound();
            }

            if (filters.TimeZoneId == null)
            {
                return BadRequest("Se requiere la zona horaria.");
            }

            // SE OBTIENE LA ZONA HORARIA PROPORCIONADA
            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(filters.TimeZoneId);
            }
            catch (Exception)
            {
                return BadRequest("La zona horaria proporcionada no se encontró o no existe. Intente de nuevo más tarde o contacte a un administrador.");
            }
            #endregion

            DateTime fechaInicial = filters.FechaMes.HasValue
                ? new DateTime(filters.FechaMes.Value.Year, filters.FechaMes.Value.Month, 1)
                : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            DateTime fechaFinal = filters.FechaMes.HasValue
                ? new DateTime(fechaInicial.Year, fechaInicial.Month, DateTime
                    .DaysInMonth(fechaInicial.Year, fechaInicial.Month))
                : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime
                    .DaysInMonth(fechaInicial.Year, fechaInicial.Month));

            IEnumerable<IEnumerable<AsistenciaReporteExcel>> queryList = await GetReportesGeneralesExcel(
                parameters, filters, timeZone, fechaInicial, fechaFinal);

            if (!queryList.Any())
            {
                return NoContent();
            }

            string rootPath = $"{_environment.WebRootPath}/docs";
            string nomenclaturaDelMes = fechaInicial.ToString("MMMM yyyy").ToUpper();
            int cantidadDiasEnMes = DateTime.DaysInMonth(fechaInicial.Year, fechaInicial.Month);

            WorkBook documento = cantidadDiasEnMes switch
            {
                28 => WorkBook.Load($"{rootPath}/NOAM_REPORTES_28.xlsx"),
                29 => WorkBook.Load($"{rootPath}/NOAM_REPORTES_29.xlsx"),
                30 => WorkBook.Load($"{rootPath}/NOAM_REPORTES_30.xlsx"),
                31 => WorkBook.Load($"{rootPath}/NOAM_REPORTES_31.xlsx"),
                _ => throw new NotImplementedException()
            };
            WorkSheet reporte = documento.GetWorkSheet("Hoja1");

            reporte.Name = nomenclaturaDelMes;
            reporte.Rows[0].Columns[2].StringValue = queryList.First().First().Servicio;
            reporte.Rows[2].Columns[4].StringValue = nomenclaturaDelMes;

            foreach (var group in queryList.Select((values, index) => (values, index)))
            {
                if (group.values.Any())
                {
                    reporte.Rows[4 + group.index].Columns[0].Value = group.values.First().Usuario;
                    reporte.Rows[4 + group.index].Columns[1].Value = group.values.First().Nombre;
                    reporte.Rows[4 + group.index].Columns[2].Value = group.values.First().Apellido;

                    // HACER UN FOR POR CADA DIA Y SACAR EL DIA DE LA LISTA QUE APLIQUE PARA
                    // GUARDAR EL REGISTRO
                    for (int dia = 0; dia < cantidadDiasEnMes; dia++)
                    {
                        // SI ALGUN DIA DE LA FECHA DE LOS REGISTROS ES EL DIA ACTUAL DE LA ITERACION
                        if (group.values.Any(a => a.FechaEntrada.Day == dia + 1))
                        {
                            if (group.values.Any(a => a.FechaEntrada.Day == dia + 1
                                && a.FechaSalida.HasValue))
                            {
                                reporte.Rows[4 + group.index].Columns[4 + dia].Value = "X";
                            }
                            else
                            {
                                reporte.Rows[4 + group.index].Columns[4 + dia].Value = "·";
                            }
                        }
                        else
                        {
                            reporte.Rows[4 + group.index].Columns[4 + dia].Value = "F";
                        }
                    }
                }
            }

            byte[] response = documento.ToBinary();

            documento.Close();

            return File(response, "application/octet-stream",
                $"Reporte {nomenclaturaDelMes}.xlsx");
        }

        #region Funciones para obtener reportes
        private async Task<IEnumerable<IEnumerable<AsistenciaReporteExcel>>> GetReportesGeneralesExcel(
            SearchParameters parameters, AsistenciaFilterParameters filters, TimeZoneInfo timeZone,
            DateTime fechaInicial, DateTime fechaFinal)
        {
            //IEnumerable<IEnumerable<AsistenciaReporteExcel>> query;

            try
            {
                // INTENTAR FILTRAR CON ZONA HORARIA DE IANA
                IEnumerable<IEnumerable<AsistenciaReporteExcel>> query = await _context.Asistencias
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdUsuarioNavigation)
                    .Sort(parameters.OrderBy!)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id) >= fechaInicial)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id) <= fechaFinal)
                    .GroupBy(a => a.IdUsuario)
                    .Select(g => g.Select(a => new AsistenciaReporteExcel
                    {
                        Usuario = a.IdUsuarioNavigation.UserName,
                        Nombre = a.IdUsuarioNavigation.Nombre,
                        Apellido = a.IdUsuarioNavigation.Apellido,
                        FechaEntrada = a.FechaEntrada,
                        FechaSalida = a.FechaSalida
                    }))
                    .ToListAsync();

                return query;
            }
            catch (SqlException)
            {
                // FILTRAR CON ZONA HORARIA FEA DE WINDOWS
                TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out string? timeZoneInfo);

                IEnumerable<IEnumerable<AsistenciaReporteExcel>> query = await _context.Asistencias
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdUsuarioNavigation)
                    .Sort(parameters.OrderBy!)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZoneInfo!) >= fechaInicial)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZoneInfo!) <= fechaFinal)
                    .GroupBy(a => a.IdUsuario)
                    .Select(g => g.Select(a => new AsistenciaReporteExcel
                    {
                        Servicio = a.IdSucursalNavigation.Descripcion,
                        Usuario = a.IdUsuarioNavigation.UserName,
                        Nombre = a.IdUsuarioNavigation.Nombre,
                        Apellido = a.IdUsuarioNavigation.Apellido,
                        FechaEntrada = a.FechaEntrada,
                        FechaSalida = a.FechaSalida
                    }))
                    .ToListAsync();

                return query;
            }
        }

        private async Task<IEnumerable<AsistenciaGeneralDTO>> GetReportesGenerales(
            SearchParameters parameters, AsistenciaFilterParameters filters, TimeZoneInfo timeZone,
            DateTime fechaInicial, DateTime fechaFinal)
        {
            try
            {
                IEnumerable<AsistenciaGeneralDTO> query = await _context.Asistencias
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdUsuarioNavigation)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id) >= fechaInicial)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id) <= fechaFinal)
                    .Sort(parameters.OrderBy!)
                    .GroupBy(a => a.IdUsuario)
                    .Select(g => g.Select(a => new AsistenciaGeneralDTO
                    {
                        Username = a.IdUsuarioNavigation.UserName,
                        UsuarioNombre = a.IdUsuarioNavigation.Nombre,
                        UsuarioApellido = a.IdUsuarioNavigation.Apellido,
                        MinutosLaborados = g.Sum(c => !c.FechaSalida.HasValue ? 0
                            : EF.Functions.DateDiffMinute(c.FechaEntrada, c.FechaSalida.Value))
                    })
                    .First())
                    .ToListAsync();

                return query;
            }
            catch (Exception)
            {
                TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out string? windowsTimeZoneId);

                IEnumerable<AsistenciaGeneralDTO> query = await _context.Asistencias
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdUsuarioNavigation)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, windowsTimeZoneId!) >= fechaInicial)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, windowsTimeZoneId!) <= fechaFinal)
                    .Sort(parameters.OrderBy!)
                    .GroupBy(a => a.IdUsuario)
                    .Select(g => g.Select(a => new AsistenciaGeneralDTO
                    {
                        Username = a.IdUsuarioNavigation.UserName,
                        UsuarioNombre = a.IdUsuarioNavigation.Nombre,
                        UsuarioApellido = a.IdUsuarioNavigation.Apellido,
                        MinutosLaborados = g.Sum(c => !c.FechaSalida.HasValue ? 0
                            : EF.Functions.DateDiffMinute(c.FechaEntrada, c.FechaSalida.Value))
                    })
                    .First())
                    .ToListAsync();

                return query;
            }
        }

        private async Task<IEnumerable<AsistenciaPersonalDTO>> GetReportesPersonales(
            SearchParameters parameters, AsistenciaFilterParameters filters, TimeZoneInfo timeZone,
            DateTime fechaInicial, DateTime fechaFinal, ApplicationUser user)
        {
            try
            {
                IEnumerable<AsistenciaPersonalDTO> query = await _context.Asistencias
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdUsuarioNavigation)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => a.IdUsuario == user.Id)
                    .Sort(parameters.OrderBy!)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id) >= fechaInicial)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id) <= fechaFinal)
                    .Select(a => new AsistenciaPersonalDTO
                    {
                        Username = a.IdUsuarioNavigation.UserName,
                        NombreUsuario = a.IdUsuarioNavigation.Nombre,
                        ApellidoUsuario = a.IdUsuarioNavigation.Apellido,
                        NombreSucursal = a.IdSucursalNavigation.Descripcion,
                        FechaEntrada = EF.Functions.AtTimeZone(a.FechaEntrada, timeZone.Id).DateTime,
                        FechaSalida = !a.FechaSalida.HasValue ? a.FechaSalida
                            : EF.Functions.AtTimeZone(a.FechaSalida.Value, timeZone.Id).DateTime,
                        MinutosLaborados = !a.FechaSalida.HasValue ? 0
                            : EF.Functions.DateDiffMinute(a.FechaEntrada, a.FechaSalida.Value)
                    })
                    .ToListAsync();

                return query;
            }
            catch (SqlException)
            {
                TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out string? windowsTimeZoneId);

                IEnumerable<AsistenciaPersonalDTO> query = await _context.Asistencias
                    .Include(a => a.IdSucursalNavigation)
                    .Include(a => a.IdUsuarioNavigation)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => a.IdUsuario == user.Id)
                    .Sort(parameters.OrderBy!)
                    .Where(a => filters.ServicioId.HasValue && a.IdSucursal == filters.ServicioId)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, windowsTimeZoneId!) >= fechaInicial)
                    .Where(a => EF.Functions.AtTimeZone(a.FechaEntrada, windowsTimeZoneId!) <= fechaFinal)
                    .Select(a => new AsistenciaPersonalDTO
                    {
                        Username = a.IdUsuarioNavigation.UserName,
                        NombreUsuario = a.IdUsuarioNavigation.Nombre,
                        ApellidoUsuario = a.IdUsuarioNavigation.Apellido,
                        NombreSucursal = a.IdSucursalNavigation.Descripcion,
                        FechaEntrada = EF.Functions.AtTimeZone(a.FechaEntrada, windowsTimeZoneId!).DateTime,
                        FechaSalida = !a.FechaSalida.HasValue ? a.FechaSalida
                            : EF.Functions.AtTimeZone(a.FechaSalida.Value, windowsTimeZoneId!).DateTime,
                        MinutosLaborados = !a.FechaSalida.HasValue ? 0
                            : EF.Functions.DateDiffMinute(a.FechaEntrada, a.FechaSalida.Value)
                    })
                    .ToListAsync();

                return query;
            }
        }
        #endregion

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
            // SI EL SERVICIO ESTA DESHABILITADO
            if (!servicio.Habilitado)
            {
                return BadRequest("No se permiten registros de asistencia para este servicio. Consulte a un administrador.");
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

        private bool AsistenciaExists(Guid id)
        {
            return (_context.Asistencias?.Any(e => e.IdUsuario == id)).GetValueOrDefault();
        }
    }

    public static class AsistenciaExtensions
    {
        public static IQueryable<Asistencia> Search(this IQueryable<Asistencia> asistencias, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return asistencias;

            return null!;
        }

        public static IQueryable<Asistencia> Sort(this IQueryable<Asistencia> asistencias, string orderByString)
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
                "fecha" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.FechaEntrada),
                "fecha" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.FechaEntrada),

                "username" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.IdUsuarioNavigation.UserName),
                "username" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.IdUsuarioNavigation.UserName),

                "nombre" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.IdUsuarioNavigation.Nombre),
                "nombre" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.IdUsuarioNavigation.Nombre),

                "apellido" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.IdUsuarioNavigation.Apellido),
                "apellido" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.IdUsuarioNavigation.Apellido),

                "sucursal" when orderDirection == "ascending"
                    => asistencias.OrderBy(s => s.IdUsuarioNavigation.UserName),
                "sucursal" when orderDirection == "descending"
                    => asistencias.OrderByDescending(s => s.IdUsuarioNavigation.UserName),

                _ => asistencias.OrderByDescending(s => s.FechaEntrada),
            };
        }
    }

}
