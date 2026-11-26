using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 14;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    // Do not use the __Host- prefix: local HTTP development cannot issue a Secure cookie.
    options.Cookie.Name = "SummaryStudio.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("UserAdministration", policy => policy.RequireRole("SystemAdministrator", "UserAdministrator"));
    options.AddPolicy("SystemAdministration", policy => policy.RequireRole("SystemAdministrator"));
    options.AddPolicy("ProcessingAdministration", policy => policy.RequireRole("SystemAdministrator", "ProcessingAdministrator", "Analyst"));
    options.AddPolicy("AuditAccess", policy => policy.RequireRole("SystemAdministrator", "Auditor"));
});
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddHostedService<AuditRetentionWorker>();
builder.Services.AddScoped<AiProviderService>();
builder.Services.AddScoped<AzureDocumentIntelligenceService>();
builder.Services.AddScoped<DocumentTextExtractor>();
builder.Services.AddScoped<ScreeningProcessorService>();
builder.Services.AddScoped<VisionScreeningExtractor>();

var dataProtectionPath = Path.GetFullPath(
    builder.Configuration["Storage:DataProtectionPath"] ?? "../storage/keys",
    builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataProtectionPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("SummaryStudio");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Do not expose stack traces, database paths, or framework internals to browser users.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UnhandledRequest");
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    logger.LogError(feature?.Error, "Unhandled request error for {Path}", context.Request.Path);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "The request could not be completed.",
        Detail = "Please try again. If the problem continues, contact a system administrator with the time of the error."
    });
}));

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (builder.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await Program.BootstrapIdentityAsync(scope.ServiceProvider, builder.Configuration, app.Logger);
}

app.Run();

public partial class Program
{
    public static async Task BootstrapIdentityAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = new[] { "SystemAdministrator", "UserAdministrator", "ProcessingAdministrator", "Analyst", "Reviewer", "Auditor", "ReadOnly" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded) throw new InvalidOperationException($"Could not create role {role}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
    }

    var username = configuration["BootstrapAdmin:UserName"]?.Trim();
    var password = configuration["BootstrapAdmin:Password"];
    var email = configuration["BootstrapAdmin:Email"]?.Trim();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
    {
        logger.LogWarning("No bootstrap administrator was created. Set BootstrapAdmin__UserName, BootstrapAdmin__Email, and BootstrapAdmin__Password before the first secure deployment.");
        return;
    }

    var existing = await userManager.FindByNameAsync(username);
    if (existing is not null) return;
    var admin = new ApplicationUser { UserName = username, Email = email, DisplayName = "System Administrator", EmailConfirmed = true, MustChangePassword = true };
    var create = await userManager.CreateAsync(admin, password);
    if (!create.Succeeded) throw new InvalidOperationException($"Could not create bootstrap administrator: {string.Join(", ", create.Errors.Select(error => error.Description))}");
    var roleResult = await userManager.AddToRoleAsync(admin, "SystemAdministrator");
    if (!roleResult.Succeeded) throw new InvalidOperationException($"Could not assign bootstrap administrator role: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
    logger.LogWarning("Bootstrap SystemAdministrator account {UserName} was created. Remove BootstrapAdmin__Password from the deployment environment.", username);
    }
}
