using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myApp.SqlIdentity.Data;

var builder = WebApplication.CreateBuilder(args);

// Always add service defaults for Aspire integration (it's safe even in standalone mode)
builder.AddServiceDefaults();

// Add services to the container.
// Check if running in Aspire (identitydb connection will be available)
// If not, fall back to SQLite
var aspireConnectionExists = builder.Configuration.GetConnectionString("identitydb") != null;

if (aspireConnectionExists)
{
    // Running in Aspire - use SQL Server container (LocalDB for dev, SQL Azure for production)
    builder.AddSqlServerDbContext<ApplicationDbContext>("identitydb");
}
else
{
    // Running standalone - use SQLite
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString, 
            sqliteOptions => sqliteOptions.MigrationsHistoryTable("__EFMigrationsHistory", "sqlite")));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Log which database mode we're using
var logger = app.Services.GetRequiredService<ILogger<Program>>();
if (aspireConnectionExists)
{
    logger.LogInformation("Running in Aspire mode with SQL Server (LocalDB for dev, SQL Azure for production)");
    
    // In Aspire mode, ensure database and tables are created
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Use EnsureCreated for SQL Server in Aspire mode (simpler than migrations)
        context.Database.EnsureCreated();
        logger.LogInformation("SQL Server database created successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating database in Aspire mode");
        // Don't throw here - let the app start and handle database errors gracefully
    }
}
else
{
    logger.LogInformation("Running in standalone mode with SQLite database");
    
    // Ensure SQLite database is created
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
