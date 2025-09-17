
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clientes = await _service.GetAllAsync();
            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cliente = await _service.GetByIdAsync(id);
            if (cliente == null) return NotFound();
            return Ok(cliente);
        }



        // OPCIÓN 2: Usar el email como identificador alternativo
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var cliente = await _service.GetByEmailAsync(email);
            if (cliente == null) return NotFound();
            return Ok(cliente);
        }

        // OPCIÓN 2b: Create que usa email para el location
        /*
        [HttpPost]
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
        */

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteUpdateDto dto)
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
                var updatedCliente = await _service.UpdateAsync(id, dto);
                if (updatedCliente == null) return NotFound();
                return Ok(updatedCliente);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { error = "Cliente no encontrado." });
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        // DELETE por email (alternativa usando identificador único)
        [HttpDelete("by-email/{email}")]
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

        // Buscar clientes por nombre
        [HttpGet("search/{nombre}")]
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

        // Verificar si existe cliente por email
        [HttpHead("by-email/{email}")]
        public async Task<IActionResult> ExistsByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest();

                var exists = await _service.ExistsByEmailAsync(email);
                return exists ? Ok() : NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }
    }
}
