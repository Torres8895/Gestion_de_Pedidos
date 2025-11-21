
using Microsoft.EntityFrameworkCore;
using Gestion_de_Pedidos.DataBase;
using Gestion_de_Pedidos.Models;
using static Gestion_de_Pedidos.Dto.ClienteDto;

namespace Gestion_de_Pedidos.Service
{
    public class ClientesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ContinuousLogger _logger;

        public ClientesService(ApplicationDbContext context, ContinuousLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ClienteReadDto>> GetAllAsync(string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.Activo == true)
                    .Select(c => new ClienteReadDto
                    {
                        Nombre = c.Nombre,
                        Email = c.Email
                    })
                    .ToListAsync();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Se consultaron {clientes.Count} clientes activos",
                    sql,
                    "Éxito"
                );

                return clientes;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar clientes: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<ClienteReadDto?> GetByEmailAsync(string email, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var cliente = await _context.Clientes
                    .Where(c => c.Email == email && c.Activo == true)
                    .FirstOrDefaultAsync();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                if (cliente == null)
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de consultar cliente inexistente con Email={email}",
                        sql,
                        "Error"
                    );
                }
                else
                {
                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Cliente {cliente.Nombre} consultado correctamente",
                        sql,
                        "Éxito"
                    );
                }

                return cliente == null ? null : new ClienteReadDto
                {
                    Nombre = cliente.Nombre,
                    Email = cliente.Email
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al consultar cliente por email: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<ClienteReadDto> CreateAsync(ClienteCreateDto clienteDto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var existeEmail = await _context.Clientes
                    .AnyAsync(c => c.Email == clienteDto.Email);

                if (existeEmail)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de crear cliente duplicado: {clienteDto.Email}",
                        sql,
                        "Error"
                    );
                    throw new InvalidOperationException("Ese email ya esta registrado.");
                }

                var cliente = new Cliente
                {
                    Nombre = clienteDto.Nombre,
                    Email = clienteDto.Email,
                    Activo = true
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Cliente {cliente.Nombre} creado correctamente",
                    sqlFinal,
                    "Éxito"
                );

                return new ClienteReadDto
                {
                    Nombre = cliente.Nombre,
                    Email = cliente.Email
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
                    $"Error al crear cliente: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<ClienteReadDto?> UpdateAsync(string email, ClienteUpdateDto clienteDto, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);

                if (cliente == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de actualizar cliente inexistente con Email={email}",
                        sql,
                        "Error"
                    );
                    return null;
                }

                cliente.Nombre = clienteDto.Nombre;
                cliente.Email = clienteDto.Email;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true ||
                        ex.InnerException?.Message.Contains("duplicate key") == true)
                    {
                        var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                        await _logger.CompletarLogDesdeServicio(
                            logId,
                            $"Intento de actualizar cliente a email duplicado: {clienteDto.Email}",
                            sql,
                            "Error"
                        );
                        throw new InvalidOperationException("Ya existe otro cliente con ese email.");
                    }
                    throw;
                }

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Cliente {cliente.Nombre} actualizado correctamente",
                    sqlFinal,
                    "Éxito"
                );

                return new ClienteReadDto
                {
                    Nombre = cliente.Nombre,
                    Email = cliente.Email
                };
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al actualizar cliente: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<bool?> DeleteByEmailAsync(string email, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);

                if (cliente == null)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"Intento de eliminar cliente inexistente con Email={email}",
                        sql,
                        "Error"
                    );
                    return null;
                }

                var tienePedidosActivos = await _context.CabeceraPedidos
                    .AnyAsync(p => p.ClienteId == cliente.Id && p.Estado != "Cancelado");

                if (tienePedidosActivos)
                {
                    var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                    await _logger.CompletarLogDesdeServicio(
                        logId,
                        $"No se puede eliminar el cliente {cliente.Nombre} porque tiene pedidos asociados",
                        sql,
                        "Error"
                    );
                    return false;
                }

                cliente.Activo = false;
                await _context.SaveChangesAsync();

                var sqlFinal = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Cliente {cliente.Nombre} eliminado correctamente",
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
                    $"Error al eliminar cliente: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                return await _context.Clientes
                    .AnyAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<ClienteReadDto>> SearchByNameAsync(string nombre, string logId)
        {
            SqlCaptureInterceptor.IniciarCaptura();

            try
            {
                var clientes = (await _context.Clientes
                    .Where(c => c.Activo == true)
                    .ToListAsync())
                    .Where(c => c.Nombre.ToLower().Contains(nombre.ToLower()))
                    .OrderBy(c => c.Nombre)
                    .Select(c => new ClienteReadDto
                    {
                        Nombre = c.Nombre,
                        Email = c.Email
                    })
                    .ToList();

                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Se encontraron {clientes.Count} clientes que contienen '{nombre}'",
                    sql,
                    "Éxito"
                );

                return clientes;
            }
            catch (Exception ex)
            {
                var sql = SqlCaptureInterceptor.ObtenerSqlCapturado();

                await _logger.CompletarLogDesdeServicio(
                    logId,
                    $"Error al buscar clientes por nombre: {ex.Message}",
                    sql,
                    "Error"
                );
                throw;
            }
        }
    }
}