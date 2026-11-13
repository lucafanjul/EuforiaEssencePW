using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// BASE DE DATOS
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// IDENTITY
builder.Services
    .AddDefaultIdentity<Usuario>(options =>
    {
        // Registro e inicio de sesión
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        // Contraseñas
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        // Bloqueo temporal por intentos fallidos
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<ErroresIdentityEspañol>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC Y PÁGINAS DE IDENTITY
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ACCESO AL USUARIO ACTUAL PARA LA AUDITORÍA
builder.Services.AddHttpContextAccessor();

// CARRITO EN SESIÓN
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddHttpClient<EuforiaEssencePW.Services.GeminiService>(
    cliente =>
    {
        cliente.Timeout = TimeSpan.FromSeconds(90);
    });
builder.Services.AddScoped<
    EuforiaEssencePW.Services.RegistroErroresService>();
var app = builder.Build();

// MANEJO DE ERRORES
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseMiddleware<
    EuforiaEssencePW.Middleware.RegistroErroresMiddleware>();
// CONFIGURACIÓN HTTP
app.UseHttpsRedirection();

// Permite servir imágenes subidas durante la ejecución.
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// CREACIÓN DE ROLES
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<Usuario>>();

    string[] roles = { "Administrador", "Cliente" };

    foreach (var rol in roles)
    {
        if (!await roleManager.RoleExistsAsync(rol))
        {
            var resultado = await roleManager.CreateAsync(
                new IdentityRole(rol));

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }
    }
    var emailAdminDemo = builder.Configuration["Tienda:AdminDemo:Email"]?.Trim();
    var claveAdminDemo = builder.Configuration["Tienda:AdminDemo:Clave"];

    if (app.Environment.IsProduction() &&
        !string.IsNullOrWhiteSpace(emailAdminDemo) &&
        !string.IsNullOrWhiteSpace(claveAdminDemo))
    {
        var adminDemo = await userManager.FindByEmailAsync(emailAdminDemo);

        if (adminDemo == null)
        {
            adminDemo = new Usuario
            {
                UserName = emailAdminDemo,
                Email = emailAdminDemo,
                EmailConfirmed = true,
                Nombre = "Administrador",
                Apellido = "Demo",
                FechaAlta = DateTime.UtcNow,
                LockoutEnabled = true
            };

            var creacion = await userManager.CreateAsync(adminDemo, claveAdminDemo);

            if (!creacion.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", creacion.Errors.Select(e => e.Description)));
            }

            var asignacion = await userManager.AddToRoleAsync(adminDemo, "Administrador");

            if (!asignacion.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", asignacion.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(adminDemo, "Administrador"))
        {
            throw new InvalidOperationException(
                "El correo del administrador de demo ya pertenece a una cuenta sin ese rol.");
        }
    }
    // CUENTAS DE PRUEBA PARA DESARROLLO
    if (app.Environment.IsDevelopment())
    {
        // ADMINISTRADOR
        const string adminEmail = "admin@euforia.com";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new Usuario
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Nombre = "Administrador",
                Apellido = "Euforia",
                FechaAlta = DateTime.UtcNow,
                LockoutEnabled = true
            };

            var resultado = await userManager.CreateAsync(
                admin, "Admin123!");

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            var resultado = await userManager.AddToRoleAsync(
                admin, "Administrador");

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }

        // CLIENTE
        const string clienteEmail = "cliente@euforia.com";

        var cliente = await userManager.FindByEmailAsync(clienteEmail);

        if (cliente == null)
        {
            cliente = new Usuario
            {
                UserName = clienteEmail,
                Email = clienteEmail,
                EmailConfirmed = true,
                Nombre = "Cliente",
                Apellido = "Prueba",
                FechaAlta = DateTime.UtcNow,
                LockoutEnabled = true
            };

            var resultado = await userManager.CreateAsync(
                cliente, "Cliente123!");

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(cliente, "Cliente"))
        {
            var resultado = await userManager.AddToRoleAsync(
                cliente, "Cliente");

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }
    }
}

// ARCHIVOS ESTÁTICOS DE LA APLICACIÓN
app.MapStaticAssets();

// RUTAS MVC
app.MapControllerRoute(
    name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// RUTAS DE IDENTITY
app.MapRazorPages()
    .WithStaticAssets();

app.Run();