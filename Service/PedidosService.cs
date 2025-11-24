using Gestion_de_Pedidos.DataBase;
using Microsoft.EntityFrameworkCore;
using Gestion_de_Pedidos.Models;
using static Gestion_de_Pedidos.Dto.PedidoDto;

namespace Gestion_de_Pedidos.Service
{
    public class PedidosService
    {
        private readonly ApplicationDbContext _context;
        private readonly ClientesService _clientesService;
        private readonly ProductoService _productoService;
        private readonly ContinuousLogger _logger;

        public PedidosService(
            ApplicationDbContext context,
            ClientesService clientesService,
            ProductoService productoService,
            ContinuousLogger logger)
        {
            _context = context;
            _clientesService = clientesService;
            _productoService = productoService;
            _logger = logger;
        }

        public async Task<PedidoCabeceraReadDto> CreateAsync(PedidoCabeceraCreateDto pedidoDto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var logIdInterno = Guid.NewGuid().ToString();

                var clienteDto = await _clientesService.GetByEmailAsync(pedidoDto.EmailCliente, logIdInterno);
                if (clienteDto == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Error: Cliente con email {pedidoDto.EmailCliente} no encontrado",
                        sql,
                        "Error"
                    );
                    throw new InvalidOperationException("Cliente no encontrado");
                }

                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Email == pedidoDto.EmailCliente && c.Activo == true);

                var productosValidos = new List<int>();
                foreach (var detalleDto in pedidoDto.Detalles)
                {
                    var productoDto = await _productoService.GetByIdAsync(detalleDto.ProductoId, logIdInterno);
                    if (productoDto == null)
                    {
                        var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                        await _logger.CompletarLogDesdeServicio(
                            logId,
                            $"Error: Producto con ID {detalleDto.ProductoId} no encontrado",
                            sql,
                            "Error"
                        );
                        throw new InvalidOperationException($"Producto con ID {detalleDto.ProductoId} no encontrado");
                    }

                    productosValidos.Add(detalleDto.ProductoId);
                }

                var productos = await _context.Productos
                    .Where(p => productosValidos.Contains(p.Id) && p.Activo)
                    .ToDictionaryAsync(p => p.Id, p => p);

                var numeroPedido = await GenerarNumeroPedidoAsync();

                var cabecera = new CabeceraPedido
                {
                    NumeroPedido = numeroPedido,
                    FechaPedido = DateTime.Now,
                    Estado = "Pendiente",
                    ClienteId = cliente.Id
                };

                _context.CabeceraPedidos.Add(cabecera);
                await _context.SaveChangesAsync();

                var total = 0m;
                var numeroDetalle = 1;

                foreach (var detalleDto in pedidoDto.Detalles)
                {
                    var producto = productos[detalleDto.ProductoId];
                    var subtotal = producto.Precio * detalleDto.Cantidad;
                    total += subtotal;

                    var detalle = new DetallePedido
                    {
                        NumeroDetalle = numeroDetalle++,
                        Cantidad = detalleDto.Cantidad,
                        CabeceraPedidoId = cabecera.Id,
                        ProductoId = detalleDto.ProductoId
                    };

                    _context.DetallePedidos.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Pedido {numeroPedido} creado exitosamente con {pedidoDto.Detalles.Count} productos",
                    sqlFinal,
                    "Éxito"
                );

                return new PedidoCabeceraReadDto
                {
                    NumeroPedido = cabecera.NumeroPedido,
                    NombreCliente = clienteDto.Nombre,
                    EmailCliente = clienteDto.Email,
                    Fecha = cabecera.FechaPedido,
                    Estado = cabecera.Estado,
                    Total = total
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al crear pedido: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<IEnumerable<PedidoCabeceraReadDto>> GetAllCabecerasAsync(string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var pedidos = await _context.CabeceraPedidos
                    .Include(c => c.Cliente)
                    .Include(c => c.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .Where(c => c.Cliente.Activo == true && c.Estado != "Cancelado")
                    .Select(c => new PedidoCabeceraReadDto
                    {
                        NumeroPedido = c.NumeroPedido,
                        NombreCliente = c.Cliente.Nombre,
                        EmailCliente = c.Cliente.Email,
                        Fecha = c.FechaPedido,
                        Estado = c.Estado,
                        Total = c.DetallesPedido
                            .Where(d => d.Producto.Activo)
                            .Sum(d => d.Producto.Precio * d.Cantidad)
                    })
                    .ToListAsync();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Se consultaron {pedidos.Count} pedidos activos (excluyendo cancelados)",
                    sql,
                    "Éxito"
                );

                return pedidos;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar pedidos: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<PedidoCabeceraReadDto?> GetCabeceraByNumeroAsync(string numeroPedido, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var pedido = await _context.CabeceraPedidos
                    .Include(c => c.Cliente)
                    .Include(c => c.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .Where(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true && c.Estado != "Cancelado")
                    .Select(c => new PedidoCabeceraReadDto
                    {
                        NumeroPedido = c.NumeroPedido,
                        NombreCliente = c.Cliente.Nombre,
                        EmailCliente = c.Cliente.Email,
                        Fecha = c.FechaPedido,
                        Estado = c.Estado,
                        Total = c.DetallesPedido
                            .Where(d => d.Producto.Activo)
                            .Sum(d => d.Producto.Precio * d.Cantidad)
                    })
                    .FirstOrDefaultAsync();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                if (pedido != null)
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Pedido {numeroPedido} consultado correctamente",
                        sql,
                        "Éxito"
                    );
                }
                else
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Pedido {numeroPedido} no encontrado",
                        sql,
                        "Error"
                    );
                }

                return pedido;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar pedido por número: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<PedidoCabeceraReadDto?> UpdateEstadoAsync(string numeroPedido, PedidoCabeceraUpdateDto updateDto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var cabecera = await _context.CabeceraPedidos
                    .Include(c => c.Cliente)
                    .Include(c => c.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true);

                if (cabecera == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Pedido {numeroPedido} no encontrado para actualizar estado",
                        sql,
                        "Error"
                    );

                    return null;
                }

                var estadosPermitidos = new[] { "Pendiente", "Cancelado", "Completado" };

                if (!estadosPermitidos.Contains(updateDto.Estado))
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Estado inválido '{updateDto.Estado}'. Solo se permiten: Pendiente, Cancelado, Completado.",
                        sql,
                        "Error"
                    );

                    throw new InvalidOperationException(
                        $"Estado inválido '{updateDto.Estado}'. Solo se permiten: Pendiente, Cancelado, Completado."
                    );
                }

                if (cabecera.Estado == "Cancelado")
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"No se puede actualizar el pedido {numeroPedido} porque está cancelado",
                        sql,
                        "Error"
                    );

                    throw new InvalidOperationException(
                        $"No se puede actualizar el pedido {numeroPedido} porque está cancelado"
                    );
                }

                if (cabecera.Estado == "Completado")
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"No se puede actualizar el pedido {numeroPedido} porque ya está completado",
                        sql,
                        "Error"
                    );

                    throw new InvalidOperationException(
                        $"No se puede actualizar el pedido {numeroPedido} porque ya está completado"
                    );
                }


                cabecera.Estado = updateDto.Estado;
                _context.CabeceraPedidos.Update(cabecera);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Estado del pedido {numeroPedido} actualizado a '{updateDto.Estado}'",
                    sqlFinal,
                    "Éxito"
                );

                return new PedidoCabeceraReadDto
                {
                    NumeroPedido = cabecera.NumeroPedido,
                    NombreCliente = cabecera.Cliente.Nombre,
                    EmailCliente = cabecera.Cliente.Email,
                    Fecha = cabecera.FechaPedido,
                    Estado = cabecera.Estado,
                    Total = cabecera.DetallesPedido
                        .Where(d => d.Producto.Activo)
                        .Sum(d => d.Producto.Precio * d.Cantidad)
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al actualizar estado del pedido: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<PedidoCabeceraReadDto?> DeleteAsync(string numeroPedido, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var cabecera = await _context.CabeceraPedidos
                    .Include(c => c.Cliente)
                    .Include(c => c.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true);

                if (cabecera == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Pedido {numeroPedido} no encontrado para eliminar",
                        sql,
                        "Error"
                    );

                    return null;
                }

                // solo puede eliminar si está Pendiente
                if (cabecera.Estado != "Pendiente")
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"No se puede eliminar el pedido {numeroPedido} porque su estado es {cabecera.Estado}",
                        sql,
                        "Error"
                    );

                    throw new InvalidOperationException(
                        $"Solo se pueden eliminar pedidos en estado Pendiente. El pedido está en estado {cabecera.Estado}."
                    );
                }

                // Si está pendiente → se cancela
                cabecera.Estado = "Cancelado";
                _context.CabeceraPedidos.Update(cabecera);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Pedido {numeroPedido} cancelado exitosamente",
                    sqlFinal,
                    "Éxito"
                );

                return new PedidoCabeceraReadDto
                {
                    NumeroPedido = cabecera.NumeroPedido,
                    NombreCliente = cabecera.Cliente.Nombre,
                    EmailCliente = cabecera.Cliente.Email,
                    Fecha = cabecera.FechaPedido,
                    Estado = cabecera.Estado,
                    Total = cabecera.DetallesPedido
                        .Where(d => d.Producto.Activo)
                        .Sum(d => d.Producto.Precio * d.Cantidad)
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al eliminar pedido: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }


        public async Task<IEnumerable<PedidoDetalleReadDto>> GetDetallesByPedidoAsync(string numeroPedido, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var detalles = await _context.DetallePedidos
                    .Include(d => d.CabeceraPedido)
                        .ThenInclude(c => c.Cliente)
                    .Include(d => d.Producto)
                    .Where(d => d.CabeceraPedido.NumeroPedido == numeroPedido &&
                                d.CabeceraPedido.Cliente.Activo == true &&
                                d.CabeceraPedido.Estado != "Cancelado" &&
                                d.Producto.Activo == true)
                    .Select(d => new PedidoDetalleReadDto
                    {
                        numeroDetalle = d.NumeroDetalle,
                        Producto = d.Producto.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.Producto.Precio,
                        Subtotal = d.Producto.Precio * d.Cantidad
                    })
                    .ToListAsync();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                if (!detalles.Any())
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"No se encontraron detalles para el pedido {numeroPedido}",
                        sql,
                        "Error"
                    );

                    return null;
                }

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Detalles del pedido {numeroPedido} consultados correctamente. Total: {detalles.Count}",
                    sql,
                    "Éxito"
                );

                return detalles;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar detalles del pedido: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<PedidoDetalleReadDto?> CreateDetalleAsync(string numeroPedido, PedidoDetalleCreateDto detalleDto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var cabecera = await _context.CabeceraPedidos
                    .Include(c => c.Cliente)
                    .Include(c => c.DetallesPedido)
                    .FirstOrDefaultAsync(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true);

                if (cabecera == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Error: Pedido con número {numeroPedido} no encontrado",
                        sql,
                        "Error"
                    );

                    return null;
                }

                var logIdInterno = Guid.NewGuid().ToString();

                var productoDto = await _productoService.GetByIdAsync(detalleDto.ProductoId, logIdInterno);
                if (productoDto == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Error: Producto con ID {detalleDto.ProductoId} no encontrado para el pedido {numeroPedido}",
                        sql,
                        "Error"
                    );

                    throw new InvalidOperationException("Producto no encontrado");
                }

                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Id == detalleDto.ProductoId && p.Activo);

                var siguienteNumeroDetalle = cabecera.DetallesPedido.Any()
                    ? cabecera.DetallesPedido.Max(d => d.NumeroDetalle) + 1
                    : 1;

                var detalle = new DetallePedido
                {
                    NumeroDetalle = siguienteNumeroDetalle,
                    Cantidad = detalleDto.Cantidad,
                    CabeceraPedidoId = cabecera.Id,
                    ProductoId = detalleDto.ProductoId
                };

                _context.DetallePedidos.Add(detalle);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Detalle {detalle.NumeroDetalle} agregado al pedido {numeroPedido} correctamente",
                    sqlFinal,
                    "Éxito"
                );

                return new PedidoDetalleReadDto
                {
                    numeroDetalle = detalle.NumeroDetalle,
                    Producto = productoDto.Nombre,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = productoDto.Precio,
                    Subtotal = productoDto.Precio * detalle.Cantidad
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al crear detalle: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<PedidoDetalleReadDto?> UpdateDetalleAsync(string numeroPedido, int numeroDetalle, PedidoDetalleUpdateDto updateDto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var detalle = await _context.DetallePedidos
                    .Include(d => d.CabeceraPedido)
                        .ThenInclude(c => c.Cliente)
                    .Include(d => d.Producto)
                    .FirstOrDefaultAsync(d => d.CabeceraPedido.NumeroPedido == numeroPedido &&
                                             d.NumeroDetalle == numeroDetalle &&
                                             d.CabeceraPedido.Cliente.Activo == true &&
                                             d.Producto.Activo == true);

                if (detalle == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Error: Detalle {numeroDetalle} del pedido {numeroPedido} no encontrado",
                        sql,
                        "Error"
                    );

                    return null;
                }

                detalle.Cantidad = updateDto.Cantidad;
                _context.DetallePedidos.Update(detalle);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Detalle {numeroDetalle} del pedido {numeroPedido} actualizado correctamente",
                    sqlFinal,
                    "Éxito"
                );

                return new PedidoDetalleReadDto
                {
                    numeroDetalle = detalle.NumeroDetalle,
                    Producto = detalle.Producto.Nombre,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.Producto.Precio,
                    Subtotal = detalle.Producto.Precio * detalle.Cantidad
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al actualizar detalle: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        public async Task<object> DeleteDetalleAsync(string numeroPedido, int numeroDetalle, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var detalle = await _context.DetallePedidos
                    .Include(d => d.CabeceraPedido)
                        .ThenInclude(c => c.Cliente)
                    .Include(d => d.Producto)
                    .FirstOrDefaultAsync(d => d.CabeceraPedido.NumeroPedido == numeroPedido &&
                                             d.NumeroDetalle == numeroDetalle &&
                                             d.CabeceraPedido.Cliente.Activo == true &&
                                             d.CabeceraPedido.Estado == "Pendiente");

                if (detalle == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Error: Detalle {numeroDetalle} del pedido {numeroPedido} no encontrado",
                        sql,
                        "Error"
                    );

                    return new { error = true, message = "Pedido pendiente no encontrado" };
                }

                var totalDetalles = await _context.DetallePedidos
                    .CountAsync(d => d.CabeceraPedidoId == detalle.CabeceraPedidoId);

                var detalleEliminado = new PedidoDetalleReadDto
                {
                    numeroDetalle = detalle.NumeroDetalle,
                    Producto = detalle.Producto.Nombre,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.Producto.Precio,
                    Subtotal = detalle.Producto.Precio * detalle.Cantidad
                };

                if (totalDetalles == 2)
                {
                    _context.DetallePedidos.Remove(detalle);
                    await _context.SaveChangesAsync();

                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Detalle {numeroDetalle} del pedido {numeroPedido} eliminado. Advertencia: solo queda un detalle",
                        sql,
                        "Éxito"
                    );

                    return new
                    {
                        warning = true,
                        message = "ADVERTENCIA: Al eliminar el próximo detalle, se cancelará automáticamente todo el pedido.",
                        detalle = detalleEliminado,
                        detallesRestantes = 1
                    };
                }

                _context.DetallePedidos.Remove(detalle);
                await _context.SaveChangesAsync();

                if (totalDetalles == 1)
                {
                    var cabecera = await _context.CabeceraPedidos
                        .Include(c => c.Cliente)
                        .FirstOrDefaultAsync(c => c.Id == detalle.CabeceraPedidoId);

                    if (cabecera != null)
                    {
                        cabecera.Estado = "Cancelado";
                        _context.CabeceraPedidos.Update(cabecera);
                        await _context.SaveChangesAsync();

                        var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                        await _logger.CompletarLogDesdeServicio(
                            logId,
                            $"Detalle {numeroDetalle} eliminado. Pedido {numeroPedido} cancelado automáticamente",
                            sql,
                            "Éxito"
                        );

                        return new
                        {
                            pedidoCancelado = true,
                            message = "Detalle eliminado. Como era el último detalle, el pedido ha sido cancelado automáticamente.",
                            detalle = detalleEliminado,
                            pedido = new
                            {
                                numeroPedido = cabecera.NumeroPedido,
                                estado = cabecera.Estado
                            }
                        };
                    }
                }

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Detalle {numeroDetalle} del pedido {numeroPedido} eliminado exitosamente",
                    sqlFinal,
                    "Éxito"
                );

                return new
                {
                    success = true,
                    message = "Detalle eliminado exitosamente.",
                    detalle = detalleEliminado,
                    detallesRestantes = totalDetalles - 1
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al eliminar detalle: {ex.Message}",
                    sql,
                    "Error"
                );

                throw;
            }
        }

        private async Task<string> GenerarNumeroPedidoAsync()
        {
            var ultimoCabecera = await _context.CabeceraPedidos
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (ultimoCabecera == null)
            {
                return "0001";
            }

            if (int.TryParse(ultimoCabecera.NumeroPedido, out int ultimoNumero))
            {
                var siguienteNumero = ultimoNumero + 1;
                return siguienteNumero.ToString("D4");
            }

            var totalPedidos = await _context.CabeceraPedidos.CountAsync();
            return (totalPedidos + 1).ToString("D4");
        }
    }
}