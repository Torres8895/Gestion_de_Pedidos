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
            try
            {
                var productos = await _context.Productos
                    .Where(p => p.Activo)
                    .Select(p => new ProductoReadDto
                    {
                        Id = p.Id,
                        Nombre = p.Nombre.ToUpper(),
                        Precio = p.Precio
                    })
                    .ToListAsync();

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Producto",
                    Accion = "Consultar Todos",
                    Mensaje = $"Se consultaron {productos.Count} productos activos.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return productos;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Producto",
                    Accion = "Consultar Todos",
                    Mensaje = ex.Message,
                    SqlSentencia = "SELECT * FROM Productos WHERE Activo = 1",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Obtener producto activo por ID
        public async Task<ProductoReadDto?> GetByIdAsync(int id)
        {
            try
            {
                var producto = await _context.Productos
                    .Where(p => p.Id == id && p.Activo)
                    .Select(p => new ProductoReadDto
                    {
                        Id = p.Id,
                        Nombre = p.Nombre.ToUpper(),
                        Precio = p.Precio
                    })
                    .FirstOrDefaultAsync();

                if (producto == null)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Consultar por ID",
                        Mensaje = $"Intento de consultar producto inexistente con ID={id}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Consultar por ID",
                        Mensaje = $"Producto {producto.Nombre} consultado correctamente.",
                        Resultado = "Exito"
                    });
                    await _context.SaveChangesAsync();
                }

                return producto;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Producto",
                    Accion = "Consultar por ID",
                    Mensaje = ex.Message,
                    SqlSentencia = $"SELECT * FROM Productos WHERE Id={id} AND Activo=1",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Crear producto
        public async Task<ProductoReadDto?> CreateAsync(ProductoCreateDto dto)
        {
            try
            {
                // Validación de unicidad
                bool existe = await _context.Productos
                    .AnyAsync(p => p.Nombre.ToUpper() == dto.Nombre.ToUpper() && p.Activo);

                if (existe)
                {
                    // Log de negocio por error de regla
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Crear",
                        Mensaje = $"Intento de crear producto duplicado: {dto.Nombre}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();

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

                // Log de negocio por éxito
                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Producto",
                    Accion = "Crear",
                    Mensaje = $"Producto {producto.Nombre} creado correctamente.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return new ProductoReadDto
                {
                    Id = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio
                };
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Producto",
                    Accion = "Crear",
                    Mensaje = ex.Message,
                    SqlSentencia = $"INSERT Producto: Nombre={dto.Nombre}, Precio={dto.Precio}",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Actualizar producto
        public async Task<ProductoReadDto?> UpdateAsync(int id, ProductoUpdateDto dto)
        {
            try
            {
                var producto = await _context.Productos
                    .Where(p => p.Id == id && p.Activo)
                    .FirstOrDefaultAsync();

                if (producto == null)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Actualizar",
                        Mensaje = $"Intento de actualizar producto inexistente con ID={id}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();

                    return null;
                }

                // Validación de unicidad
                bool existe = await _context.Productos
                    .AnyAsync(p => p.Nombre.ToUpper() == dto.Nombre.ToUpper() && p.Id != id && p.Activo);

                if (existe)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Actualizar",
                        Mensaje = $"Intento de actualizar producto a nombre duplicado: {dto.Nombre}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();

                    throw new InvalidOperationException("Ya existe un producto con ese nombre.");
                }

                producto.Nombre = dto.Nombre.ToUpper();
                producto.Precio = dto.Precio;

                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Producto",
                    Accion = "Actualizar",
                    Mensaje = $"Producto {producto.Nombre} actualizado correctamente.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return new ProductoReadDto
                {
                    Id = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio
                };
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Producto",
                    Accion = "Actualizar",
                    Mensaje = ex.Message,
                    SqlSentencia = $"UPDATE Producto SET Nombre={dto.Nombre}, Precio={dto.Precio} WHERE Id={id}",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Eliminar producto (soft delete)
        // Retorna: true si se eliminó correctamente, false si tiene pedidos, null si no existe
        public async Task<bool?> DeleteAsync(int id)
        {
            try
            {
                var producto = await _context.Productos
                    .Where(p => p.Id == id && p.Activo)
                    .FirstOrDefaultAsync();

                if (producto == null)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Eliminar",
                        Mensaje = $"Intento de eliminar producto inexistente con ID={id}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();

                    return null;
                }

                // Validar que el producto no esté en detalles de pedidos no cancelados
                var estaEnPedidosActivos = await _context.DetallePedidos
                    .Include(d => d.CabeceraPedido)
                    .AnyAsync(d => d.ProductoId == id && d.CabeceraPedido.Estado != "Cancelado");

                if (estaEnPedidosActivos)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Producto",
                        Accion = "Eliminar",
                        Mensaje = $"No se puede eliminar el producto {producto.Nombre} porque está asociado a pedidos activos.",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();

                    return false;
                }

                producto.Activo = false;
                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Producto",
                    Accion = "Eliminar",
                    Mensaje = $"Producto {producto.Nombre} desactivado correctamente.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Producto",
                    Accion = "Eliminar",
                    Mensaje = ex.Message,
                    SqlSentencia = $"UPDATE Producto SET Activo=0 WHERE Id={id}",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }
    }
}
