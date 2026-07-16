using Microsoft.EntityFrameworkCore;
using SmartTestDataGenerator.Core.Interfaces;
using SmartTestDataGenerator.Infrastructure.Data;
using SmartTestDataGenerator.Infrastructure.Repositories;
using SmartTestDataGenerator.Infrastructure.Services;
using SmartTestDataGenerator.Application.Interfaces;
using SmartTestDataGenerator.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();

// Register AutoMapper & Services
builder.Services.AddAutoMapper(typeof(SmartTestDataGenerator.Application.Mappings.MappingProfile));
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IDataGeneratorService, DataGeneratorService>();
builder.Services.AddScoped<IExportService, ExportService>();

var app = builder.Build();

// Seed Database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Apply pending migrations or create DB if it doesn't exist
        context.Database.Migrate();
        // Seed templates
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı ilklendirilirken bir hata oluştu.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Standard .NET 8 static files middleware

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
