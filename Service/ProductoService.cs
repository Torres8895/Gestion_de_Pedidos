using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Gestion_de_Pedidos.Models;
using static Gestion_de_Pedidos.Dto.ProductoDto;
using Gestion_de_Pedidos.DataBase;

namespace Gestion_de_Pedidos.Service
{
    public class ProductoService
    {
        private readonly ApplicationDbContext _context;

        public ProductoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            try
            {
                return await _context.Productos.ToListAsync();
            }
            catch (Exception)
            {
                throw new Exception("Error inesperado al obtener todos los productos.");
            }
        }

        public async Task<Producto> GetByIdAsync(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                    throw new Exception($"Producto con ID {id} no encontrado.");

                return producto;
            }
            catch (Exception)
            {
                throw new Exception($"Error inesperado al buscar producto con ID {id}.");
            }
        }

        public async Task<Producto> CreateAsync(ProductoCreateDto dto)
        {
            // Verificación de duplicado
            if (await _context.Productos.AnyAsync(p => p.Nombre == dto.Nombre))
                throw new Exception("Ya existe un producto con ese nombre.");

            try
            {
                var producto = new Producto
                {
                    Nombre = dto.Nombre,
                    Precio = dto.Precio,
                };

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
                return producto;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
            {
                // Capturamos los errores específicos de SQL Server
                string mensaje = sqlEx.Number switch
                {
                    2627 => "Violación de clave primaria: ya existe un producto con ese ID.",
                    2601 => "Violación de índice único: ya existe un producto con ese nombre.",
                    547 => "Restricción de integridad: no se puede crear producto debido a registros relacionados.",
                    _ => $"Error de base de datos SQL ({sqlEx.Number}): {sqlEx.Message}"
                };
                throw new Exception(mensaje);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado al crear el producto: {ex.Message}");
            }
        }

        public async Task<bool> UpdateAsync(int id, ProductoUpdateDto dto)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                throw new Exception($"Producto con ID {id} no encontrado.");

            if (await _context.Productos.AnyAsync(p => p.Nombre == dto.Nombre && p.Id != id))
                throw new Exception("Ya existe otro producto con ese nombre.");

            try
            {
                producto.Nombre = dto.Nombre;
                producto.Precio = dto.Precio;
                producto.Activo = dto.Activo;

                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
            {
                string mensaje = sqlEx.Number switch
                {
                    2627 => "Violación de clave primaria: ya existe un producto con ese ID.",
                    2601 => "Violación de índice único: ya existe otro producto con ese nombre.",
                    547 => "Restricción de integridad: no se puede actualizar producto debido a registros relacionados.",
                    _ => $"Error de base de datos SQL ({sqlEx.Number}): {sqlEx.Message}"
                };
                throw new Exception(mensaje);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado al actualizar el producto: {ex.Message}");
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                throw new Exception($"Producto con ID {id} no encontrado.");

            try
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
            {
                string mensaje = sqlEx.Number switch
                {
                    547 => "Restricción de integridad: no se puede eliminar producto porque tiene registros relacionados.",
                    _ => $"Error de base de datos SQL ({sqlEx.Number}): {sqlEx.Message}"
                };
                throw new Exception(mensaje);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado al eliminar el producto: {ex.Message}");
            }
        }
    }
}
