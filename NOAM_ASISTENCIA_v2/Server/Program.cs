using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NOAM_ASISTENCIA_v2.Server;
using NOAM_ASISTENCIA_v2.Server.Data;
using NOAM_ASISTENCIA_v2.Server.Models;
using NOAM_ASISTENCIA_v2.Server.Utills.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	options.UseSqlServer(connectionString);
	options.UseOpenIddict();
});

// Register the Identity services.
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddErrorDescriber<ErroresTraducidos>()
	.AddDefaultTokenProviders()
	.AddDefaultUI();

// OpenId Connect server configuration
builder.Services.AddOpenIddict()
	// Register the OpenIddict core components.
	.AddCore(options =>
	{
		// Configure OpenIddict to use the Entity Framework Core stores and models.
		// Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
		options.UseEntityFrameworkCore()
				.UseDbContext<ApplicationDbContext>();

		/*// Enable Quartz.NET integration.
		options.UseQuartz();*/
	})
	// Register the OpenIddict server components.
	.AddServer(options =>
	{
		// Enable the authorization, logout, token and userinfo endpoints.
		options.SetAuthorizationEndpointUris("connect/authorize")
				.SetLogoutEndpointUris("connect/logout")
				.SetTokenEndpointUris("connect/token")
				.SetUserinfoEndpointUris("connect/userinfo");

		// Mark the "email", "profile" and "roles" scopes as supported scopes.
		options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

		// Note: the sample uses the code and refresh token flows but you can enable
		// the other flows if you need to support implicit, password or client credentials.
		options.AllowAuthorizationCodeFlow()
				.AllowRefreshTokenFlow();

		// Register the signing and encryption credentials.
		options.AddDevelopmentEncryptionCertificate()
				.AddDevelopmentSigningCertificate();

		// Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
		options.UseAspNetCore()
				.EnableAuthorizationEndpointPassthrough()
				.EnableLogoutEndpointPassthrough()
				.EnableStatusCodePagesIntegration()
				.EnableTokenEndpointPassthrough();
	})
	// Register the OpenIddict validation components.
	.AddValidation(options =>
	{
		// Import the configuration from the local OpenIddict server instance.
		options.UseLocalServer();

		// Register the ASP.NET Core host.
		options.UseAspNetCore();
	});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// Register the worker responsible for seeding the database.
// Note: in a real world application, this step should be part of a setup script.
builder.Services.AddHostedService<InitDB>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
