using System.Text;
using Employee_History.DappaRepo;
using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllers();

// Read CORS settings from appsettings.json
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();

// Configure CORS using settings from appsettings.json
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(corsSettings.AllowOrigins.ToArray())
              .WithMethods(corsSettings.AllowMethods.ToArray())
              .WithHeaders(corsSettings.AllowHeaders.ToArray());
    });
});

// Retrieve the secret key from configuration
var secretKey = builder.Configuration["Jwt:SecretKey"];
var keyBytes = Encoding.ASCII.GetBytes(secretKey);

// Register the secret key as a singleton service
builder.Services.AddSingleton(secretKey);

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
        ValidateAudience = false
    };
});

builder.Services.AddScoped<IDappaEmployee, DappaEmployee>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IDapperUser, DapperUser>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<LocationRange>();

// Configure connection string from appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<IImageRepository>(provider => new ImageRepository(connectionString));

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();

// Use CORS
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// CORS settings class
public class CorsSettings
{
    public string[] AllowOrigins { get; set; }
    public string[] AllowMethods { get; set; }
    public string[] AllowHeaders { get; set; }
}
