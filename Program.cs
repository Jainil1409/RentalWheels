using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Globalization;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Set Indian locale for currency formatting (₹1,00,000.00)
var indianCulture = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = indianCulture;
CultureInfo.DefaultThreadCurrentUICulture = indianCulture;

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<UnsplashService>();

builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddGoogle(options =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.ClientId = string.IsNullOrEmpty(clientId) ? "YOUR_GOOGLE_CLIENT_ID" : clientId;
        options.ClientSecret = string.IsNullOrEmpty(clientSecret) ? "YOUR_GOOGLE_CLIENT_SECRET" : clientSecret;
    });

var app = builder.Build();

// If the process was started with --migrate, apply migrations and exit (useful for Render jobs)
if (args.Contains("--migrate"))
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
            await DbSeeder.SeedAsync(db);
            logger.LogInformation("Migrations applied (--migrate) and seeding complete. Exiting.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying migrations or seeding database (--migrate).");
        }
    }

    return; // exit process after running migrations
}

// Configure Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Apply migrations and seed data on normal startup (errors are logged but don't stop the app)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying migrations or seeding database.");
        // Do not rethrow — allow the app to start so Render's health checks can surface the problem.
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
