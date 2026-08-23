using System.Text;
using System.Threading.RateLimiting;
using Employee_History.Common.Health;
using Employee_History.Common.Middleware;
using Employee_History.Common.Models;
using Employee_History.Common.Security;
using Employee_History.Features.Attendance;
using Employee_History.Features.Auth;
using Employee_History.Features.Email;
using Employee_History.Features.Images;
using Employee_History.Features.Leave;
using Employee_History.Features.Notifications;
using Employee_History.Features.PasswordReset;
using Employee_History.Features.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Configuration sources (later wins):
//   appsettings.json -> appsettings.{Env}.json -> appsettings.Local.json (git-ignored, local dev secrets) -> environment variables
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// ---------------------------------------------------------------------------
// Required settings — fail fast with a clear message instead of a stack trace.
// ---------------------------------------------------------------------------
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Missing database connection string. Set the ConnectionStrings__DefaultConnection environment variable " +
        "(or add it to appsettings.Local.json for local development).");
}

string? secretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException(
        "Missing JWT secret key. Set the Jwt__SecretKey environment variable " +
        "(or add it to appsettings.Local.json for local development).");
}

// ---------------------------------------------------------------------------
// Controllers — validation errors return the standard { success, message } envelope.
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => e.Value!.Errors[0].ErrorMessage)
                .FirstOrDefault() ?? "Invalid request.";
            return new BadRequestObjectResult(new ApiMessage(firstError, false));
        };
    });

// ---------------------------------------------------------------------------
// CORS — origins from config/env. "*" means any origin (no credentials; the
// frontend authenticates with a bearer header, not cookies).
// Pin production origins via CorsSettings__AllowOrigins__0 etc.
// ---------------------------------------------------------------------------
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>() ?? new CorsSettings();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (corsSettings.AllowOrigins == null || corsSettings.AllowOrigins.Length == 0 || corsSettings.AllowOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsSettings.AllowOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// ---------------------------------------------------------------------------
// Authentication & authorization
// ---------------------------------------------------------------------------
var keyBytes = Encoding.ASCII.GetBytes(secretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    // A1 = super admin, B2 = sub admin, C3 = staff
    options.AddPolicy("SuperAdmin", policy => policy.RequireClaim("LabRole", "A1"));
    options.AddPolicy("Admin", policy => policy.RequireClaim("LabRole", "A1", "B2"));

    // Every endpoint requires an authenticated user unless explicitly [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ---------------------------------------------------------------------------
// Rate limiting — protects login / password-reset / refresh endpoints.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------------------------------------------------------------------------
// Data access & feature services — one SqlConnection per request scope,
// disposed by the container.
// ---------------------------------------------------------------------------
builder.Services.AddScoped(_ => new SqlConnection(connectionString));
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();

builder.Services.AddHealthChecks()
    .AddCheck<SqlHealthCheck>("database");

// ---------------------------------------------------------------------------
// Swagger — controller/action <summary> docs are included from the XML file.
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FBN-DIL Attendance API", Version = "v1" });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, "Employee_History.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });
});

// HTTPS redirection only when an https port is explicitly configured.
// Locally the app binds http only, and in production (Render/Docker) TLS is
// terminated by the platform proxy — redirecting there is a no-op that just
// logs "Failed to determine the https port" warnings.
var httpsPort = builder.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT")
             ?? builder.Configuration.GetValue<int?>("HttpsRedirection:HttpsPort");
if (httpsPort.HasValue)
{
    builder.Services.AddHttpsRedirection(options => options.HttpsPort = httpsPort.Value);
}

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FBN-DIL Attendance API v1"));
}

if (httpsPort.HasValue)
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();

// Friendly root: Swagger in Development, API info elsewhere.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}
else
{
    app.MapGet("/", () => Results.Ok(new
    {
        name = "FBN-DIL Attendance API",
        status = "running",
        health = "/api/health"
    })).AllowAnonymous();
}

app.Run();

/// <summary>CORS settings bound from the CorsSettings configuration section.</summary>
public class CorsSettings
{
    public string[] AllowOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowMethods { get; set; } = Array.Empty<string>();
    public string[] AllowHeaders { get; set; } = Array.Empty<string>();
}
