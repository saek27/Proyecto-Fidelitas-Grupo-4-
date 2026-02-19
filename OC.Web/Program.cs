using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REGISTRO DE SERVICIOS (Antes de builder.Build) ---

// Configuración de Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("OC.Data")));

// Registro de Repositorios
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// CONFIGURACIÓN DE AUTENTICACIÓN (Se movió aquí arriba)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddControllersWithViews();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
//Valor Clinico
builder.Services.AddScoped<IGenericRepository<ValorClinico>, GenericRepository<ValorClinico>>();



//Documento
builder.Services.AddScoped<IGenericRepository<DocumentoExpediente>, GenericRepository<DocumentoExpediente>>();

//Peso de archivo
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 10 * 1024 * 1024; // 10 MB
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// --- LA LÍNEA FRONTERIZA (Solo una vez) ---
var app = builder.Build();

// --- 2. SEEDING DE DATOS (Después de Build, antes de los Middlewares) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        OC.Data.Context.DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar la base de datos.");
    }
}

// --- 3. CONFIGURACIÓN DEL PIPELINE (Middlewares) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // 1. Primero Routing

app.UseAuthentication(); // 2. Luego Quién eres
app.UseAuthorization();  // 3. Finalmente Qué puedes hacer

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();