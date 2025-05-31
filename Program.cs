using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using UserManagementApp.Services;

using UserManagementApp.Configuration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
    
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

// Добавляем автоматическое применение миграций
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var maxRetries = 5;
    var retryDelay = TimeSpan.FromSeconds(5);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Попытка применить миграции (Попытка {0}/{1})", i + 1, maxRetries);
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            
            // Дополнительная проверка перед миграцией
            if (!dbContext.Database.CanConnect())
            {
                throw new Exception("Нет подключения к базе данных");
            }
            
            dbContext.Database.Migrate();
            logger.LogInformation("Миграции успешно применены");
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Попытка миграции {0} не удалась", i + 1);
            if (i == maxRetries - 1)
            {
                logger.LogError("Все попытки миграции провалились");
                throw;
            }
            Thread.Sleep(retryDelay);
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
