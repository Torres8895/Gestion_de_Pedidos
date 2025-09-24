
using Microsoft.AspNetCore.Mvc;
using Gestion_de_Pedidos.Service;
using static Gestion_de_Pedidos.Dto.ClienteDto;

namespace Gestion_de_Pedidos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ClientesService _service;

        public ClientesController(ClientesService service)
        {
            _service = service;
        }

        [HttpGet("search")]
        public async Task<IActionResult> GetAll()
        {
            var clientes = await _service.GetAllAsync();
            return Ok(clientes);
        }

        // Buscar clientes por nombre
        [HttpGet("search-name/{nombre}")]
        public async Task<IActionResult> SearchByName(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return BadRequest(new { error = "El nombre es requerido." });

                var clientes = await _service.SearchByNameAsync(nombre);
                return Ok(clientes);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        //buscar cliente por email
        [HttpGet("search-email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var cliente = await _service.GetByEmailAsync(email);
            if (cliente == null) return NotFound(new { error = "Cliente no encontrado." });
            return Ok(cliente);
        }

        // alta de cliente
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { errores });
            }
            
            try
            {
                var cliente = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetByEmail), new { email = cliente.Email }, cliente);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // actualizar cliente por email
        [HttpPut("update-by-email/{email}")]
        public async Task<IActionResult> Update(string email, [FromBody] ClienteUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { errores });
            }

            try
            {
                var updatedCliente = await _service.UpdateAsync(email, dto);
                if (updatedCliente == null) return NotFound();
                return Ok(updatedCliente);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // eliminar cliente por email (soft delete)
        [HttpDelete("delete-by-email/{email}")]
        public async Task<IActionResult> DeleteByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { error = "El email es requerido." });

                var deleted = await _service.DeleteByEmailAsync(email);
                if (!deleted)
                    return NotFound(new { error = "Cliente no encontrado." });
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

    }
}
