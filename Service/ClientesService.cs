
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
            return await _context.Clientes
                .Where(c => c.Activo == true)
                .Select(c => new ClienteReadDto
                {
                    Nombre = c.Nombre,
                    Email = c.Email
                })
                .ToListAsync();
        }

        // Obtener cliente por ID
        public async Task<ClienteReadDto?> GetByIdAsync(int id)
        {
            var cliente = await _context.Clientes
                .Where(c => c.Id == id && c.Activo == true)
                .FirstOrDefaultAsync();

            if (cliente == null)
                return null;

            return new ClienteReadDto
            {
                Nombre = cliente.Nombre,
                Email = cliente.Email
            };
        }

        // Obtener cliente por email (para usar como identificador alternativo)
        public async Task<ClienteReadDto?> GetByEmailAsync(string email)
        {
            var cliente = await _context.Clientes
                .Where(c => c.Email == email && c.Activo == true)
                .FirstOrDefaultAsync();

            if (cliente == null)
                return null;

            return new ClienteReadDto
            {
                Nombre = cliente.Nombre,
                Email = cliente.Email
            };
        }

        // Crear nuevo cliente
        public async Task<ClienteReadDto> CreateAsync(ClienteCreateDto clienteDto)
        {
            // Verificar si ya existe un cliente con ese email
            var existeEmail = await _context.Clientes
                .AnyAsync(c => c.Email == clienteDto.Email && c.Activo == true);

            if (existeEmail)
                throw new InvalidOperationException("Ya existe un cliente con ese email.");

            var cliente = new Cliente
            {
                Nombre = clienteDto.Nombre,
                Email = clienteDto.Email,
                Activo = true
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return new ClienteReadDto
            {
                Nombre = cliente.Nombre,
                Email = cliente.Email
            };
        }

        // Actualizar cliente
        public async Task<ClienteReadDto?> UpdateAsync(string email, ClienteUpdateDto clienteDto)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);

            if (cliente == null)
                return null;

            cliente.Nombre = clienteDto.Nombre;
            cliente.Email = clienteDto.Email;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Manejo de error de unicidad a nivel de BD
                if (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true ||
                    ex.InnerException?.Message.Contains("duplicate key") == true)
                {
                    throw new InvalidOperationException("Ya existe otro cliente con ese email.");
                }
                throw;
            }

            return new ClienteReadDto
            {
                Nombre = cliente.Nombre,
                Email = cliente.Email
            };
        }

        // Eliminar cliente por email (soft delete)
        public async Task<bool> DeleteByEmailAsync(string email)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);

            if (cliente == null)
                return false;

            cliente.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }

        // Verificar si existe un cliente por email
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Clientes
                .AnyAsync(c => c.Email.ToLower() == email.ToLower() && c.Activo == true);
        }

        // Buscar clientes por nombre
        public async Task<IEnumerable<ClienteReadDto>> SearchByNameAsync(string nombre)
        {
            return await _context.Clientes
                .Where(c => c.Activo == true && c.Nombre.ToLower().Contains(nombre.ToLower()))
                .OrderBy(c => c.Nombre)
                .Select(c => new ClienteReadDto
                {
                    Nombre = c.Nombre,
                    Email = c.Email
                })
                .ToListAsync();
        }
    }
}
