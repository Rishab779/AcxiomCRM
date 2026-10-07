using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Database
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=acxiomcrm.db";
var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=AcxiomCRM_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerConnection);
    }
    else
    {
        options.UseSqlite(sqliteConnection);
    }
});

// 2. Configure ASP.NET Core Identity & Password Policy
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = builder.Configuration.GetValue<bool>("SecuritySettings:RequireDigit", true);
    options.Password.RequireLowercase = builder.Configuration.GetValue<bool>("SecuritySettings:RequireLowercase", true);
    options.Password.RequireUppercase = builder.Configuration.GetValue<bool>("SecuritySettings:RequireUppercase", true);
    options.Password.RequireNonAlphanumeric = builder.Configuration.GetValue<bool>("SecuritySettings:RequireNonAlphanumeric", true);
    options.Password.RequiredLength = builder.Configuration.GetValue<int>("SecuritySettings:RequiredPasswordLength", 8);

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("SecuritySettings:LockoutDurationMinutes", 15));
    options.Lockout.MaxFailedAccessAttempts = builder.Configuration.GetValue<int>("SecuritySettings:MaxFailedAccessAttempts", 5);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Configure Authentication Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// 4. Register CRM Application Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

// 5. Add Controllers and Views with Anti-forgery
builder.Services.AddControllersWithViews(options =>
{
    // Global filter or settings if needed
});

// 6. Configure Swagger/OpenAPI for REST API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AcxiomCRM REST API",
        Version = "v1",
        Description = "Production-grade RESTful API endpoints for AcxiomCRM sales management, customer intelligence, leads, and pipeline analytics."
    });
});

var app = builder.Build();

// 7. Seed Database and Initial Users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 8. Configure HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Account/AccessDenied");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Swagger Documentation UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcxiomCRM API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
