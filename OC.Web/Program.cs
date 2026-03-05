using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Data.Repositories;
using OC.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. REGISTRO DE SERVICIOS (Antes de builder.Build) ---

// Configuraciùn de Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("OC.Data")));

// Registro de Repositorios
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// CONFIGURACIùN DE AUTENTICACIùN (Se moviù aquù arriba)
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

// CIT-RF-016 Recordatorios de citas
builder.Services.Configure<RecordatorioCitasOptions>(
    builder.Configuration.GetSection(RecordatorioCitasOptions.SectionName));
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<RecordatorioCitasBackgroundService>();

// --- LA LùNEA FRONTERIZA (Solo una vez) ---
var app = builder.Build();

// --- 2. SEEDING DE DATOS (Despuùs de Build, antes de los Middlewares) ---
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
        logger.LogError(ex, "Ocurriù un error al sembrar la base de datos.");
    }
}

// --- 3. CONFIGURACIùN DEL PIPELINE (Middlewares) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // 1. Primero Routing

app.UseAuthentication(); // 2. Luego Quiùn eres
app.UseAuthorization();  // 3. Finalmente Quù puedes hacer

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();