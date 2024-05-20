using Employee_History.DappaRepo;
using Employee_History.Interface;
using Employee_History.Models;
using FluentAssertions.Common;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IDappaEmployee, DappaEmployee>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IDapperUser, DapperUser>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<LocationRange>();

// Configure connection string from appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<IImageRepository>(provider => new ImageRepository(connectionString));

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen();

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
app.UseAuthorization();

app.MapControllers();

app.Run();
