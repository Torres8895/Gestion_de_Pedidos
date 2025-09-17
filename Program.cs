using Gestion_de_Pedidos.DataBase;
using Gestion_de_Pedidos.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 📌 Configuración de la conexión a SQL Server Express
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyección de dependencias
builder.Services.AddScoped<ProductoService>();

// Controladores
builder.Services.AddControllers();

// Swagger (documentación de API) - CON VERSIÓN
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Gestión de Pedidos API",
        Version = "v1",
        Description = "API para gestión de pedidos y productos"
    });
});

// 📌 Configuración de CORS para permitir cualquier frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Permite cualquier origen
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestión de Pedidos API V1");
        c.RoutePrefix = string.Empty; // Hace que Swagger sea la página de inicio
    });
}

// 📌 Activar CORS antes de la autorización
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
