using Microsoft.EntityFrameworkCore;
using Gestion_de_Pedidos.Models;

namespace Gestion_de_Pedidos.DataBase
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
    }
}