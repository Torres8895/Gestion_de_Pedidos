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

        public PedidosService(
            ApplicationDbContext context,
            ClientesService clientesService,
            ProductoService productoService)
        {
            _context = context;
            _clientesService = clientesService;
            _productoService = productoService;
        }

        /// <summary>
        /// Crear un nuevo pedido completo (cabecera + detalles) como una transacción
        /// </summary>
        public async Task<PedidoCabeceraReadDto> CreateAsync(PedidoCabeceraCreateDto pedidoDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validar que el cliente existe usando ClientesService
                var clienteDto = await _clientesService.GetByEmailAsync(pedidoDto.EmailCliente);
                if (clienteDto == null)
                    throw new InvalidOperationException("Cliente no encontrado");

                // Obtener el cliente completo para usar su ID
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Email == pedidoDto.EmailCliente && c.Activo == true);

                // Validar que todos los productos existen usando ProductoService
                var productosValidos = new List<int>();
                foreach (var detalleDto in pedidoDto.Detalles)
                {
                    var productoDto = await _productoService.GetByIdAsync(detalleDto.ProductoId);
                    if (productoDto == null)
                        throw new InvalidOperationException($"Producto con ID {detalleDto.ProductoId} no encontrado");

                    productosValidos.Add(detalleDto.ProductoId);
                }

                // Obtener productos completos para calcular totales
                var productos = await _context.Productos
                    .Where(p => productosValidos.Contains(p.Id) && p.Activo)
                    .ToDictionaryAsync(p => p.Id, p => p);

                // Generar número de pedido único
                var numeroPedido = await GenerarNumeroPedidoAsync();

                // Crear cabecera del pedido
                var cabecera = new CabeceraPedido
                {
                    NumeroPedido = numeroPedido,
                    FechaPedido = DateTime.Now,
                    Estado = "Pendiente",
                    ClienteId = cliente.Id
                };

                _context.CabeceraPedidos.Add(cabecera);
                await _context.SaveChangesAsync();

                // Crear detalles del pedido
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

                // Retornar el pedido creado
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
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Obtener todas las cabeceras de pedidos (excluye cancelados)
        /// </summary>
        public async Task<IEnumerable<PedidoCabeceraReadDto>> GetAllCabecerasAsync()
        {
            return await _context.CabeceraPedidos
                .Include(c => c.Cliente)
                .Include(c => c.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .Where(c => c.Cliente.Activo == true && c.Estado != "Cancelado") // Excluir cancelados
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
        }

        /// <summary>
        /// Obtener una cabecera de pedido por número de pedido (excluye cancelados)
        /// </summary>
        public async Task<PedidoCabeceraReadDto?> GetCabeceraByNumeroAsync(string numeroPedido)
        {
            return await _context.CabeceraPedidos
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
        }

        /// <summary>
        /// Actualizar el estado de un pedido
        /// </summary>
        public async Task<PedidoCabeceraReadDto?> UpdateEstadoAsync(string numeroPedido, PedidoCabeceraUpdateDto updateDto)
        {
            var cabecera = await _context.CabeceraPedidos
                .Include(c => c.Cliente)
                .Include(c => c.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true);

            if (cabecera == null)
                return null;

            cabecera.Estado = updateDto.Estado;
            _context.CabeceraPedidos.Update(cabecera);
            await _context.SaveChangesAsync();

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

        /// <summary>
        /// Eliminar un pedido completo (soft delete - marca como inactivo)
        /// </summary>
        public async Task<PedidoCabeceraReadDto?> DeleteAsync(string numeroPedido)
        {
            var cabecera = await _context.CabeceraPedidos
                .Include(c => c.Cliente)
                .Include(c => c.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true);

            if (cabecera == null)
                return null;

            // Soft delete: cambiar estado a "Cancelado" en lugar de eliminar físicamente
            cabecera.Estado = "Cancelado";
            _context.CabeceraPedidos.Update(cabecera);
            await _context.SaveChangesAsync();

            return new PedidoCabeceraReadDto
            {
                NumeroPedido = cabecera.NumeroPedido,
                NombreCliente = cabecera.Cliente.Nombre,
                EmailCliente = cabecera.Cliente.Email,
                Fecha = cabecera.FechaPedido,
                Estado = cabecera.Estado, // "Cancelado"
                Total = cabecera.DetallesPedido
                    .Where(d => d.Producto.Activo == true)
                    .Sum(d => d.Producto.Precio * d.Cantidad)
            };
        }

        /// <summary>
        /// Obtener todos los detalles de un pedido específico
        /// </summary>
        public async Task<IEnumerable<PedidoDetalleReadDto>> GetDetallesByPedidoAsync(string numeroPedido)
        {
            var detalles = await _context.DetallePedidos
                .Include(d => d.CabeceraPedido)
                    .ThenInclude(c => c.Cliente)
                .Include(d => d.Producto)
                .Where(d => d.CabeceraPedido.NumeroPedido == numeroPedido &&
                           d.CabeceraPedido.Cliente.Activo == true &&
                           d.CabeceraPedido.Estado == "Pendiente" &&
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
            if (!detalles.Any())
                return null;
            //throw new InvalidOperationException("Pedido pendiente no encontrado");

            return detalles;
        }

        /// <summary>
        /// Agregar un detalle a un pedido existente
        /// </summary>
        public async Task<PedidoDetalleReadDto?> CreateDetalleAsync(string numeroPedido, PedidoDetalleCreateDto detalleDto)
        {
            var cabecera = await _context.CabeceraPedidos
                .Include(c => c.Cliente)
                .Include(c => c.DetallesPedido)
                .FirstOrDefaultAsync(c => c.NumeroPedido == numeroPedido && c.Cliente.Activo == true);

            if (cabecera == null)
                return null;

            // Validar que el producto existe usando ProductoService
            var productoDto = await _productoService.GetByIdAsync(detalleDto.ProductoId);
            if (productoDto == null)
                throw new InvalidOperationException("Producto no encontrado");

            // Obtener producto completo para el precio
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == detalleDto.ProductoId && p.Activo);

            // Generar el siguiente número de detalle
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

            return new PedidoDetalleReadDto
            {
                numeroDetalle = detalle.NumeroDetalle,
                Producto = productoDto.Nombre,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = productoDto.Precio,
                Subtotal = productoDto.Precio * detalle.Cantidad
            };
        }

        /// <summary>
        /// Actualizar la cantidad de un detalle específico
        /// </summary>
        public async Task<PedidoDetalleReadDto?> UpdateDetalleAsync(string numeroPedido, int numeroDetalle, PedidoDetalleUpdateDto updateDto)
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
                return null;

            detalle.Cantidad = updateDto.Cantidad;
            _context.DetallePedidos.Update(detalle);
            await _context.SaveChangesAsync();

            return new PedidoDetalleReadDto
            {
                numeroDetalle = detalle.NumeroDetalle,
                Producto = detalle.Producto.Nombre,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.Producto.Precio,
                Subtotal = detalle.Producto.Precio * detalle.Cantidad
            };
        }

        /// <summary>
        /// Eliminar un detalle específico de un pedido
        /// Con lógica automática de soft delete de cabecera si no quedan detalles
        /// </summary>
        public async Task<object> DeleteDetalleAsync(string numeroPedido, int numeroDetalle)
        {
            var detalle = await _context.DetallePedidos
                .Include(d => d.CabeceraPedido)
                    .ThenInclude(c => c.Cliente)
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(d => d.CabeceraPedido.NumeroPedido == numeroPedido &&
                                         d.NumeroDetalle == numeroDetalle &&
                                         d.CabeceraPedido.Cliente.Activo == true &&
                                         d.CabeceraPedido.Estado == "Pendiente"); // Verificación de pendiente

            if (detalle == null)
                return new { error = true, message = "Pedido pendiente no encontrado" };

            // Verificar cuántos detalles quedarían después de eliminar este
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

            // Si solo queda 1 detalle, advertir al usuario
            if (totalDetalles == 2) // 2 porque aún no hemos eliminado el actual
            {
                // Eliminar el detalle
                _context.DetallePedidos.Remove(detalle);
                await _context.SaveChangesAsync();
                return new
                {
                    warning = true,
                    message = "ADVERTENCIA: Al eliminar el próximo detalle, se cancelará automáticamente todo el pedido.",
                    detalle = detalleEliminado,
                    detallesRestantes = 1
                };
            }

            // Eliminar el detalle
            _context.DetallePedidos.Remove(detalle);
            await _context.SaveChangesAsync();

            // Si era el último detalle, hacer soft delete automático de la cabecera
            if (totalDetalles == 1) // Era el único detalle
            {
                var cabecera = await _context.CabeceraPedidos
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.Id == detalle.CabeceraPedidoId);

                if (cabecera != null)
                {
                    cabecera.Estado = "Cancelado";
                    _context.CabeceraPedidos.Update(cabecera);
                    await _context.SaveChangesAsync();

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

            // Eliminación normal de detalle
            return new
            {
                success = true,
                message = "Detalle eliminado exitosamente.",
                detalle = detalleEliminado,
                detallesRestantes = totalDetalles - 1
            };
        }

        /// <summary>
        /// Generar un número de pedido único de 4 dígitos
        /// </summary>
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

            // Si no se puede parsear, generar basado en el conteo total
            var totalPedidos = await _context.CabeceraPedidos.CountAsync();
            return (totalPedidos + 1).ToString("D4");
        }
    }
}