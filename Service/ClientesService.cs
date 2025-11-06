
using Microsoft.EntityFrameworkCore;
using Gestion_de_Pedidos.DataBase;
using Gestion_de_Pedidos.Models;
using static Gestion_de_Pedidos.Dto.ClienteDto;

namespace Gestion_de_Pedidos.Service
{
    public class ClientesService
    {

        private readonly ApplicationDbContext _context;

        public ClientesService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtener todos los clientes activos   
        public async Task<IEnumerable<ClienteReadDto>> GetAllAsync()
        {
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

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Cliente",
                    Accion = "Consultar Todos",
                    Mensaje = $"Se consultaron {clientes.Count} clientes activos.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return clientes;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Consultar Todos",
                    Mensaje = ex.Message,
                    SqlSentencia = "SELECT * FROM Clientes WHERE Activo = 1",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Obtener cliente por email (para usar como identificador alternativo)
        public async Task<ClienteReadDto?> GetByEmailAsync(string email)
        {
            try
            {
                var cliente = await _context.Clientes
                    .Where(c => c.Email == email && c.Activo == true)
                    .FirstOrDefaultAsync();

                if (cliente == null)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Cliente",
                        Accion = "Consultar por Email",
                        Mensaje = $"Intento de consultar cliente inexistente con Email={email}",
                        Resultado = "Error"
                    });
                }
                else
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Cliente",
                        Accion = "Consultar por Email",
                        Mensaje = $"Cliente {cliente.Nombre} consultado correctamente.",
                        Resultado = "Exito"
                    });
                }

                await _context.SaveChangesAsync();

                return cliente == null ? null : new ClienteReadDto
                {
                    Nombre = cliente.Nombre,
                    Email = cliente.Email
                };
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Consultar por Email",
                    Mensaje = ex.Message,
                    SqlSentencia = $"SELECT * FROM Clientes WHERE Email='{email}' AND Activo=1",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Crear nuevo cliente
        public async Task<ClienteReadDto> CreateAsync(ClienteCreateDto clienteDto)
        {
            try
            {
                var existeEmail = await _context.Clientes
                    .AnyAsync(c => c.Email == clienteDto.Email);

                if (existeEmail)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Cliente",
                        Accion = "Crear",
                        Mensaje = $"Intento de crear cliente duplicado: {clienteDto.Email}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();
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

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Cliente",
                    Accion = "Crear",
                    Mensaje = $"Cliente {cliente.Nombre} creado correctamente.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

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
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Crear",
                    Mensaje = ex.Message,
                    SqlSentencia = $"INSERT Cliente: Nombre={clienteDto.Nombre}, Email={clienteDto.Email}",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Actualizar cliente
        public async Task<ClienteReadDto?> UpdateAsync(string email, ClienteUpdateDto clienteDto)
        {
            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);

                if (cliente == null)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Cliente",
                        Accion = "Actualizar",
                        Mensaje = $"Intento de actualizar cliente inexistente con Email={email}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();
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
                        _context.NegocioLog.Add(new NegocioLog
                        {
                            Entidad = "Cliente",
                            Accion = "Actualizar",
                            Mensaje = $"Intento de actualizar cliente a email duplicado: {clienteDto.Email}",
                            Resultado = "Error"
                        });
                        await _context.SaveChangesAsync();
                        throw new InvalidOperationException("Ya existe otro cliente con ese email.");
                    }
                    throw;
                }

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Cliente",
                    Accion = "Actualizar",
                    Mensaje = $"Cliente {cliente.Nombre} actualizado correctamente.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return new ClienteReadDto
                {
                    Nombre = cliente.Nombre,
                    Email = cliente.Email
                };
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Actualizar",
                    Mensaje = ex.Message,
                    SqlSentencia = $"UPDATE Cliente SET Nombre={clienteDto.Nombre}, Email={clienteDto.Email} WHERE Email={email}",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Eliminar cliente por email (soft delete)
        // Retorna: true si se eliminó, false si tiene pedidos, null si no existe
        public async Task<bool?> DeleteByEmailAsync(string email)
        {
            try
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);

                if (cliente == null)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Cliente",
                        Accion = "Eliminar",
                        Mensaje = $"Intento de eliminar cliente inexistente con Email={email}",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();
                    return null;
                }

                // Validar que el cliente no tenga pedidos no cancelados
                var tienePedidosActivos = await _context.CabeceraPedidos
                    .AnyAsync(p => p.ClienteId == cliente.Id && p.Estado != "Cancelado");

                if (tienePedidosActivos)
                {
                    _context.NegocioLog.Add(new NegocioLog
                    {
                        Entidad = "Cliente",
                        Accion = "Eliminar",
                        Mensaje = $"No se puede eliminar el cliente {cliente.Nombre} porque tiene pedidos asociados.",
                        Resultado = "Error"
                    });
                    await _context.SaveChangesAsync();
                    return false;
                }

                cliente.Activo = false;
                await _context.SaveChangesAsync();

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Cliente",
                    Accion = "Eliminar",
                    Mensaje = $"Cliente {cliente.Nombre} eliminado correctamente.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Eliminar",
                    Mensaje = ex.Message,
                    SqlSentencia = $"UPDATE Cliente SET Activo=0 WHERE Email={email}",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Verificar si existe un cliente por email
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                return await _context.Clientes
                    .AnyAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Verificar existencia por Email",
                    Mensaje = ex.Message,
                    SqlSentencia = $"SELECT COUNT(*) FROM Clientes WHERE Email={email} AND Activo=1",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // Buscar clientes por nombre
        public async Task<IEnumerable<ClienteReadDto>> SearchByNameAsync(string nombre)
        {
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

                _context.NegocioLog.Add(new NegocioLog
                {
                    Entidad = "Cliente",
                    Accion = "Buscar por Nombre",
                    Mensaje = $"Se encontraron {clientes.Count} clientes que contienen '{nombre}'.",
                    Resultado = "Exito"
                });
                await _context.SaveChangesAsync();

                return clientes;
            }
            catch (Exception ex)
            {
                _context.SqlLog.Add(new SqlLog
                {
                    Entidad = "Cliente",
                    Accion = "Buscar por Nombre",
                    Mensaje = ex.Message,
                    SqlSentencia = "SELECT * FROM Clientes WHERE Activo=1",
                    Resultado = "Error"
                });
                await _context.SaveChangesAsync();
                throw;
            }
        }
    }
}
