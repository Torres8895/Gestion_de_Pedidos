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

        // Obtener todos los productos activos
        public async Task<IEnumerable<ProductoReadDto>> GetAllAsync()
        {
            return await _context.Productos
                .Where(p => p.Activo) 
                .Select(p => new ProductoReadDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre.ToUpper(),
                    Precio = p.Precio
                })
                .ToListAsync();
        }

        // Obtener producto activo por ID
        public async Task<ProductoReadDto?> GetByIdAsync(int id)
        {
            return await _context.Productos
                .Where(p => p.Id == id && p.Activo)
                .Select(p => new ProductoReadDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre.ToUpper(),
                    Precio = p.Precio
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ProductoReadDto> CreateAsync(ProductoCreateDto dto)
        {
            var producto = new Producto
            {
                Nombre = dto.Nombre.ToUpper(),
                Precio = dto.Precio,
                Activo = true
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // Devolver DTO de lectura usando LINQ
            return await _context.Productos
                .Where(p => p.Id == producto.Id)
                .Select(p => new ProductoReadDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre.ToUpper(),
                    Precio = p.Precio
                })
                .FirstOrDefaultAsync()!;
        }


        public async Task<ProductoReadDto?> UpdateAsync(int id, ProductoUpdateDto dto)
        {
            var producto = await _context.Productos
                .Where(p => p.Id == id && p.Activo)
                .FirstOrDefaultAsync();

            if (producto == null)
                return null;

            producto.Nombre = dto.Nombre.ToUpper();
            producto.Precio = dto.Precio;

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            // Proyección a DTO usando LINQ
            return await _context.Productos
                .Where(p => p.Id == producto.Id)
                .Select(p => new ProductoReadDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre.ToUpper(),
                    Precio = p.Precio
                })
                .FirstOrDefaultAsync();
        }


        public async Task<ProductoReadDto?> DeleteAsync(int id)
        {
            var producto = await _context.Productos
                .Where(p => p.Id == id && p.Activo)
                .FirstOrDefaultAsync();

            if (producto == null)
                return null;

            producto.Activo = false;
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            // Proyección final a DTO
            return await _context.Productos
                .Where(p => p.Id == producto.Id)
                .Select(p => new ProductoReadDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre.ToUpper(),
                    Precio = p.Precio
                })
                .FirstOrDefaultAsync();
        }
    }
}
