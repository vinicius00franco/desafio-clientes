using ApiBackend.Data;
using ApiBackend.Data.Services;
using ApiBackend.Features.Clientes.Repositories;
using ApiBackend.Features.Clientes.Services;
using ApiBackend.Services.External;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Add DbContext with SQL Server
builder.Services.AddDbContext<ContextoApp>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add HttpClient for ViaCEP
builder.Services.AddHttpClient<ViaCepService>();
builder.Services.AddScoped<IViaCepService, ViaCepService>();

// Add application services
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ClienteService>();

// Add database initialization service
builder.Services.AddScoped<DatabaseInitializationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInitializer.Initialize();
}

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

// Tornar a classe Program acessível para testes de integração
public partial class Program { }
