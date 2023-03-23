using NOAM_ASISTENCIA_v2.Server.Models;
using NOAM_ASISTENCIA_v2.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using NOAM_ASISTENCIA_v2.Server.Models.Utils;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NOAM_ASISTENCIA_v2.Server;

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
		await CreateAppDescriptor(scope, _configuration.GetSection("AppBaseUrl"));
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
				new TempTurno(id: 1, descripcion: "Ninguno", descripcionCorta: "Ninguno"),
				new TempTurno(id: 2, descripcion: "Lunes a viernes de 8:00 a 14:00", descripcionCorta: "L - V | M" ),
				new TempTurno (id: 3, descripcion: "Lunes a viernes de 14:00 a 22:00", descripcionCorta: "L - V | V")
			};

			var servicios = new List<TempServicio>()
			{
				new TempServicio(id: 1, descripcion: "3974 BOWLING MONTERREY"),
				new TempServicio(id: 2, descripcion: "4010 SMART FIT PLAZA TITAN MTY"),
				new TempServicio(id: 3, descripcion : "4011 SMART FIT MULTIPLAZA MTY"),
				new TempServicio(id: 4, descripcion: "4012 SMART FIT PLAZA FIESTA MTY"),
				new TempServicio(id: 5, descripcion : "4017 SMART FIT STA CATARINA MTY")
			};

			await CreateTurnosIfDontExist(dbcontext, turnos);
			await CreateSucursalesIfDontExist(dbcontext, servicios);
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
						DescripcionCorta = turno.DescripcionCorta
					}
				);
			}
		}

		dbcontext.SaveChangesWithIdentityInsert<Turno>();
	}

	private async Task CreateSucursalesIfDontExist(ApplicationDbContext dbcontext, IEnumerable<TempServicio> servicios)
	{
		foreach (TempServicio servicio in servicios)
		{
			if (!await dbcontext.SucursalServicios.Where(x => x.Id == servicio.Id).AnyAsync())
			{
				await dbcontext.SucursalServicios.AddAsync(
					new SucursalServicio
					{
						Id = servicio.Id,
						Descripcion = servicio.Descripcion
					}
				);
			}
		}

		dbcontext.SaveChangesWithIdentityInsert<SucursalServicio>();
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
			}

			if (oUser != null && user.Roles.Any())
			{
				await usermanager.AddToRolesAsync(oUser, user.Roles);
			}
		}
	}
	#endregion

	#region APP_DESCRIPTOR
	private async Task CreateAppDescriptor(AsyncServiceScope scope, IConfigurationSection request)
	{
		var openIddictManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

		if (await openIddictManager.FindByClientIdAsync("blazor-client") is null)
		{
			await openIddictManager.CreateAsync(new OpenIddictApplicationDescriptor
			{
				ClientId = "blazor-client",
				ConsentType = ConsentTypes.Explicit,
				DisplayName = "Blazor Client Application",
				Type = ClientTypes.Public,
				PostLogoutRedirectUris = { new Uri($"{request.Value}/authentication/logout-callback") },
				RedirectUris = { new Uri($"{request.Value}/authentication/login-callback") },
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
		public string DescripcionCorta { get; } = null!;

		public TempTurno(int id, string descripcion, string descripcionCorta)
		{
			Id = id;
			Descripcion = descripcion;
			DescripcionCorta = descripcionCorta;
		}
	}

	private class TempServicio
	{
		public int Id { get; }
		public string Descripcion { get; } = null!;

		public TempServicio(int id, string descripcion)
		{
			Id = id;
			Descripcion = descripcion;
		}
	}
	#endregion
}