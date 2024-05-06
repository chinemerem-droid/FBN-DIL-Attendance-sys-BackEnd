using Employee_History.DappaRepo;
using Employee_History.Interface;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IDappaEmployee, DappaEmployee>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IDapperUser, DapperUser>();
builder.Services.AddScoped<IDeviceInfoRepository, DeviceInfoRepository>();

string connectionString = "Server=JULIUSBOT;Database=Attendance system;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False";
builder.Services.AddScoped<IImageRepository>(provider => new ImageRepository(connectionString));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
