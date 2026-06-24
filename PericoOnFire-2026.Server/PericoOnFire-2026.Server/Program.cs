using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.BD.Datos.Entity;
using PericoOnFire_2026.Repositorio.Repositorios;
using PericoOnFire_2026.Repositorio.Seguridad;
using PericoOnFire_2026.Server.Client.Pages;
using PericoOnFire_2026.Server.Components;
using PericoOnFire_2026.Server.Components.Account;
using PericoOnFire_2026.Servicio.ServicioHttp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IHttpServicio, HttpServicio>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7202");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
var connectionString = builder.Configuration.GetConnectionString("ConSqlServer") ?? throw new InvalidOperationException("El string de conexion no existe.");
builder.Services.AddDbContext<MiDbContext>(options =>
    options.UseSqlServer(connectionString));

//Registrar los repositorios

builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<ISubcategoriaRepositorio, SubcategoriaRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<IComandaRepositorio, ComandaRepositorio>();
builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<IServicioSeguridad, ServicioSeguridad>();
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    //Esto es lo de roles que tenemos que agregar para el identity
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MiDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();
//Supuestamente aca tendria que crear los roles al arrancar la aplicacion

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Administracion", "Cocina", "Delivery", "Mozo" };
    foreach (var rol in roles)
    {
        if (!await roleManager.RoleExistsAsync(rol))
            await roleManager.CreateAsync(new IdentityRole(rol));
    }

    // Usuario admin semilla — solo se crea si no existe
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var emailAdmin = "admin@pericoonfire.com";
    var adminExiste = await userManager.FindByEmailAsync(emailAdmin);
    if (adminExiste == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = emailAdmin,
            Email = emailAdmin,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin1234!");
        await userManager.AddToRoleAsync(adminUser, "Administracion");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapSwagger().AllowAnonymous();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets()
    .AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PericoOnFire_2026.Server.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.Run();
