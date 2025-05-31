using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using UserManagementApp.Services;
using UserManagementApp.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Получаем строку подключения из конфигурации
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Настраиваем DbContext с политикой повторных попыток
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, 
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));
    
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

// Добавляем автоматическое применение миграций с улучшенной обработкой ошибок
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var maxRetries = 10; // Увеличиваем количество попыток
    var retryDelay = TimeSpan.FromSeconds(10); // Увеличиваем задержку между попытками
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to apply migrations (Attempt {0}/{1})", i + 1, maxRetries);
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            
            // Проверяем подключение к базе данных
            if (dbContext.Database.CanConnect())
            {
                logger.LogInformation("Database connection established, applying migrations...");
                dbContext.Database.Migrate();
                logger.LogInformation("Migrations applied successfully");
                break;
            }
            else
            {
                logger.LogWarning("Cannot connect to database, retrying...");
                throw new Exception("Database connection failed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration attempt {0} failed", i + 1);
            if (i == maxRetries - 1)
            {
                logger.LogError("All migration attempts failed");
                throw;
            }
            await Task.Delay(retryDelay); // Используем асинхронное ожидание
        }
    }
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