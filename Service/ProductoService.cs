using Microsoft.EntityFrameworkCore;
using Gestion_de_Pedidos.Models;
using static Gestion_de_Pedidos.Dto.ProductoDto;
using Gestion_de_Pedidos.DataBase;

namespace Gestion_de_Pedidos.Service
{
    public class ProductoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ContinuousLogger _logger;

        public ProductoService(ApplicationDbContext context, ContinuousLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductoReadDto>> GetAllAsync(string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

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

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Se consultaron {productos.Count} productos activos",
                    sql,
                    "Éxito"
                );

                return productos;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar productos: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<ProductoReadDto?> GetByIdAsync(int id, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

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

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                if (producto == null)
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de consultar producto inexistente con ID={id}",
                        sql,
                        "Error"
                    );
                }
                else
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Producto {producto.Nombre} consultado correctamente",
                        sql,
                        "Éxito"
                    );
                }

                return producto;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar producto por ID: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<ProductoReadDto?> CreateAsync(ProductoCreateDto dto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                bool existe = await _context.Productos
                    .AnyAsync(p => p.Nombre.ToUpper() == dto.Nombre.ToUpper() && p.Activo);

                if (existe)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de crear producto duplicado: {dto.Nombre}",
                        sql,
                        "Error"
                    );

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

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Producto {producto.Nombre} creado correctamente",
                    sqlFinal,
                    "Éxito"
                );

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
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al crear producto: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<ProductoReadDto?> UpdateAsync(int id, ProductoUpdateDto dto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var producto = await _context.Productos
                    .Where(p => p.Id == id && p.Activo)
                    .FirstOrDefaultAsync();

                if (producto == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de actualizar producto inexistente con ID={id}",
                        sql,
                        "Error"
                    );

                    return null;
                }

                bool existe = await _context.Productos
                    .AnyAsync(p => p.Nombre.ToUpper() == dto.Nombre.ToUpper() && p.Id != id && p.Activo);

                if (existe)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de actualizar producto a nombre duplicado: {dto.Nombre}",
                        sql,
                        "Error"
                    );

                    throw new InvalidOperationException("Ya existe un producto con ese nombre.");
                }

                producto.Nombre = dto.Nombre.ToUpper();
                producto.Precio = dto.Precio;

                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Producto {producto.Nombre} actualizado correctamente",
                    sqlFinal,
                    "Éxito"
                );

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
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al actualizar producto: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<bool?> DeleteAsync(int id, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var producto = await _context.Productos
                    .Where(p => p.Id == id && p.Activo)
                    .FirstOrDefaultAsync();

                if (producto == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de eliminar producto inexistente con ID={id}",
                        sql,
                        "Error"
                    );

                    return null;
                }

                var estaEnPedidosActivos = await _context.DetallePedidos
                    .Include(d => d.CabeceraPedido)
                    .AnyAsync(d => d.ProductoId == id && d.CabeceraPedido.Estado != "Cancelado");

                if (estaEnPedidosActivos)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"No se puede eliminar el producto {producto.Nombre} porque está asociado a pedidos activos",
                        sql,
                        "Error"
                    );

                    return false;
                }

                producto.Activo = false;
                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Producto {producto.Nombre} desactivado correctamente",
                    sqlFinal,
                    "Éxito"
                );

                return true;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al eliminar producto: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }
    }
}
