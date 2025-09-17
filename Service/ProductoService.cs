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

        public async Task<ProductoReadDto?> CreateAsync(ProductoCreateDto dto)
        {
            // Validación de unicidad (ignora mayúsculas/minúsculas)
            bool existe = await _context.Productos
                .AnyAsync(p => p.Nombre.ToUpper() == dto.Nombre.ToUpper() && p.Activo);

            if (existe)
            {
                throw new InvalidOperationException("Ya existe un producto con ese nombre.");
            }

            var producto = new Producto
            {
                Nombre = dto.Nombre.ToUpper(),
                Precio = dto.Precio,
                Activo = true
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return new ProductoReadDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            };
        }


        public async Task<ProductoReadDto?> UpdateAsync(int id, ProductoUpdateDto dto)
        {
            var producto = await _context.Productos
                .Where(p => p.Id == id && p.Activo)
                .FirstOrDefaultAsync();

            if (producto == null)
                return null;

            // Validación de unicidad, ignorando el registro actual
            bool existe = await _context.Productos
                .AnyAsync(p => p.Nombre.ToUpper() == dto.Nombre.ToUpper() && p.Id != id && p.Activo);

            if (existe)
            {
                throw new InvalidOperationException("Ya existe un producto con ese nombre.");
            }

            producto.Nombre = dto.Nombre.ToUpper();
            producto.Precio = dto.Precio;

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            return new ProductoReadDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            };
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
