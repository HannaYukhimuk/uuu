using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using UserManagementApp.Services;
using UserManagementApp.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Получаем строку подключения из конфигурации
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));




builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();

var app = builder.Build();

// Асинхронное применение миграций
try
{
    await ApplyMigrationsAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while applying migrations");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/User/Login");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Index}/{id?}");

app.MapRazorPages(); 

app.Run();

async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var maxRetries = 15; // Увеличиваем количество попыток
    var retryDelay = TimeSpan.FromSeconds(15); // Увеличиваем задержку
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to connect to database (Attempt {Attempt}/{MaxAttempts})", i + 1, maxRetries);
            
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            
            // Проверяем подключение к базе данных
            if (await dbContext.Database.CanConnectAsync())
            {
                logger.LogInformation("Database connection established, applying migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");
                return;
            }
            
            logger.LogWarning("Cannot connect to database, retrying...");
            throw new Exception("Database connection failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection attempt {Attempt} failed", i + 1);
            if (i == maxRetries - 1)
            {
                logger.LogError("All connection attempts failed");
                throw;
            }
            await Task.Delay(retryDelay);
        }
    }
}