using NOAM_ASISTENCIA_V2.Server.Models;
using NOAM_ASISTENCIA_V2.Server.Utils;
using NOAM_ASISTENCIA_V2.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NOAM_ASISTENCIA_V2.Server;

public class InitDB : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public InitDB(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var usermanager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var rolemanager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InitDB>>();

        await InitializeDatabase(dbcontext, usermanager, rolemanager, logger);
        await CreateAppDescriptors(scope, logger);
    }

    #region INITIALIZE_DB
    private async Task InitializeDatabase(ApplicationDbContext dbcontext, UserManager<ApplicationUser> usermanager, RoleManager<ApplicationRole> rolemanager, ILogger<InitDB> logger)
    {
        if (await TryToMigrate(dbcontext, logger))
        {
            await SeedDefaultData(dbcontext, logger);
            await SeedDefaultUsersAndRoles(usermanager, rolemanager, logger);
        }
    }

    private async Task<bool> TryToMigrate(ApplicationDbContext dbcontext, ILogger<InitDB> logger)
    {
        try
        {
            await dbcontext.Database.MigrateAsync();
        }
        catch (Exception)
        {
            logger.Log(LogLevel.Error, "Error al migrar la base de datos.");

            return false;
        }

        return true;
    }
    #endregion

    #region SEED_DEFAULT_DATA
    private async Task SeedDefaultData(ApplicationDbContext dbcontext, ILogger<InitDB> logger)
    {
        try
        {
            var turnos = new List<TempTurno>()
            {
                new TempTurno(id: 1, descripcion: "Ninguno"),
                new TempTurno(id: 2, descripcion: "Lunes-Viernes 06:00-16:00"),
                new TempTurno(id: 3, descripcion: "Lunes-Viernes 12:00-22:00"),
                new TempTurno(id: 4, descripcion: "Lunes,Miercoles,Viernes 06:00-16:00"),
                new TempTurno(id: 5, descripcion: "Lunes,Miercoles,Viernes 12:00-22:00"),
                new TempTurno(id: 6, descripcion: "Martes,Jueves,Sábado 06:00-16:00"),
                new TempTurno(id: 7, descripcion: "Martes,Jueves,Sábado 12:00-22:00")
            };

            var servicios = new List<TempServicio>()
            {
                new TempServicio(id: 1, codigoId: "3974", descripcion: "BOWLING MONTERREY"),
                new TempServicio(id: 2, codigoId: "4010", descripcion: "SMART FIT PLAZA TITAN MTY"),
                new TempServicio(id: 3, codigoId: "4011", descripcion: "SMART FIT MULTIPLAZA MTY"),
                new TempServicio(id: 4, codigoId: "4012", descripcion: "SMART FIT PLAZA FIESTA MTY"),
                new TempServicio(id: 5, codigoId: "4017", descripcion: "SMART FIT STA CATARINA MTY")
            };

            await CreateTurnosIfDontExist(dbcontext, turnos);
            await CreateServiciosIfDontExist(dbcontext, servicios);
        }
        catch (Exception)
        {
            logger.Log(LogLevel.Error, "Error al insertar datos predefinidos.");
        }
    }

    private async Task CreateTurnosIfDontExist(ApplicationDbContext dbcontext, IEnumerable<TempTurno> turnos)
    {
        foreach (TempTurno turno in turnos)
        {
            if (!await dbcontext.Turnos.Where(x => x.Id == turno.Id).AnyAsync())
            {
                await dbcontext.Turnos.AddAsync(
                    new Turno
                    {
                        Id = turno.Id,
                        Descripcion = turno.Descripcion,
                        Habilitado = true
                    }
                );
            }
        }

        dbcontext.SaveChangesWithIdentityInsert<Turno>();
    }

    private async Task CreateServiciosIfDontExist(ApplicationDbContext dbcontext, IEnumerable<TempServicio> servicios)
    {
        foreach (TempServicio servicio in servicios)
        {
            if (!await dbcontext.Servicios.Where(x => x.Id == servicio.Id).AnyAsync())
            {
                await dbcontext.Servicios.AddAsync(
                    new Servicio
                    {
                        Id = servicio.Id,
                        CodigoId = servicio.CodigoId,
                        Descripcion = servicio.Descripcion,
                        Habilitado = true
                    }
                );
            }
        }

        dbcontext.SaveChangesWithIdentityInsert<Servicio>();
    }
    #endregion

    #region SEED_USERS_AND_ROLES
    private async Task SeedDefaultUsersAndRoles(UserManager<ApplicationUser> usermanager, RoleManager<ApplicationRole> rolemanager, ILogger<InitDB> logger)
    {
        try
        {
            var adminRole = "Administrador";
            var adminUser = new TempUser(name: "administrador", email: "", password: "Pa55w.rd", nombre: "Usuario", apellido: "Administrador", roles: new List<string>() { adminRole });

            var gerenteRole = "Gerente";
            var gerenteUser = new TempUser(name: "gerente", email: "", password: "Pa55w.rd", nombre: "Usuario", apellido: "Gerente", roles: new List<string>() { gerenteRole });

            var intendenteRole = "Intendente";
            var intendenteUser = new TempUser(name: "intendente", email: "", password: "Pa55w.rd", nombre: "Usuario", apellido: "Itendente", roles: new List<string>() { intendenteRole });

            var roles = new List<string>() { adminRole, gerenteRole, intendenteRole };
            var users = new List<TempUser>() { adminUser, gerenteUser, intendenteUser };

            await CreateRolesIfDontExist(rolemanager, roles);
            await CreateUsersIfDontExist(usermanager, users);
        }
        catch (Exception)
        {
            logger.Log(LogLevel.Error, "Error al crear roles y usuarios.");
        }
    }

    private static async Task CreateRolesIfDontExist(RoleManager<ApplicationRole> rolemanager, IEnumerable<string> roles)
    {
        foreach (string role in roles)
        {
            ApplicationRole? oRole = await rolemanager.FindByNameAsync(role);

            if (oRole == null)
            {
                oRole = new ApplicationRole()
                {
                    Id = Guid.NewGuid(),
                    Name = role
                };

                await rolemanager.CreateAsync(oRole);
            }
        }
    }

    private static async Task CreateUsersIfDontExist(UserManager<ApplicationUser> usermanager, IEnumerable<TempUser> users)
    {
        foreach (TempUser user in users)
        {
            ApplicationUser? oUser = await usermanager.FindByNameAsync(user.Name);

            if (oUser == null)
            {
                oUser = new ApplicationUser()
                {
                    Id = Guid.NewGuid(),
                    UserName = user.Name,
                    Email = user.Email,
                    Nombre = user.Nombre,
                    Apellido = user.Apellido,
                    IdTurno = 1
                };

                // CREATE USER
                await usermanager.CreateAsync(oUser, user.Password);
                //CONFIRM EMAIL
                var token = await usermanager.GenerateEmailConfirmationTokenAsync(oUser);
                await usermanager.ConfirmEmailAsync(oUser, token);

                if (user.Roles.Any())
                {
                    await usermanager.AddToRolesAsync(oUser, user.Roles);
                }
            }
        }
    }
    #endregion

    #region APP_DESCRIPTOR
    private async Task CreateAppDescriptors(AsyncServiceScope scope, ILogger<InitDB> logger)
    {
        var openIddictManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        try
        {
            TempAppDescriptor[] appDescriptorSection = _configuration
                .GetSection("OpenIdAppDescriptors").Get<TempAppDescriptor[]>();

            foreach (TempAppDescriptor appDescriptor in appDescriptorSection)
            {
                // SI NO SE ENCUENTRA EL DESCRIPTOR CON SU ID, LO CREA
                if (await openIddictManager.FindByClientIdAsync(appDescriptor.ClientId) is null)
                {
                    await openIddictManager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = appDescriptor.ClientId,
                        ConsentType = ConsentTypes.Explicit,
                        DisplayName = "Blazor Client Application",
                        Type = ClientTypes.Public,
                        PostLogoutRedirectUris =
                        {
                            new Uri($"{appDescriptor.BaseUrl}/authentication/logout-callback")
                        },
                        RedirectUris =
                        {
                            new Uri($"{appDescriptor.BaseUrl}/authentication/login-callback")
                        },
                        Permissions =
                        {
                            Permissions.Endpoints.Authorization,
                            Permissions.Endpoints.Logout,
                            Permissions.Endpoints.Token,
                            Permissions.GrantTypes.AuthorizationCode,
                            Permissions.GrantTypes.RefreshToken,
                            Permissions.ResponseTypes.Code,
                            Permissions.Scopes.Email,
                            Permissions.Scopes.Profile,
                            Permissions.Scopes.Roles
                        },
                        Requirements =
                        {
                            Requirements.Features.ProofKeyForCodeExchange
                        }
                    });
                }
            }
        }
        catch (Exception)
        {
            logger.Log(LogLevel.Error, "Error al intentar crear App Descriptors");
        }
    }
    #endregion

    #region TEMP_CLASSES
    private class TempUser
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public IEnumerable<string> Roles { get; set; } = null!;

        public TempUser(string name, string email, string password, string nombre, string apellido, IEnumerable<string> roles)
        {
            Name = name;
            Email = email;
            Password = password;
            Nombre = nombre;
            Apellido = apellido;
            Roles = roles;
        }
    }

    private class TempTurno
    {
        public int Id { get; }
        public string Descripcion { get; } = null!;

        public TempTurno(int id, string descripcion)
        {
            Id = id;
            Descripcion = descripcion;
        }
    }

    private class TempServicio
    {
        public int Id { get; }
        public string CodigoId { get; } = null!;
        public string Descripcion { get; } = null!;

        public TempServicio(int id, string codigoId, string descripcion)
        {
            Id = id;
            CodigoId = codigoId;
            Descripcion = descripcion;
        }
    }

    private class TempAppDescriptor
    {
        public string ClientId { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
    }
    #endregion
}