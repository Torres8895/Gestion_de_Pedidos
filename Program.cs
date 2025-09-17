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

// Swagger (documentación de API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    app.UseSwaggerUI();
}

// 📌 Activar CORS antes de la autorización
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
