
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
        private readonly ContinuousLogger _logger;

        public ClientesController(ClientesService service, ContinuousLogger logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> GetAll()
        {
            var logId = IniciarLog();

            try
            {
                var clientes = await _service.GetAllAsync(logId);
                await _logger.FinalizarLog(logId);
                return Ok(clientes);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetAll: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpGet("search-name/{nombre}")]
        public async Task<IActionResult> SearchByName(string nombre)
        {
            var logId = IniciarLog();

            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    await _logger.RegistrarErrorController(logId, "Validación fallida: El nombre es requerido");
                    await _logger.FinalizarLog(logId);
                    return BadRequest(new { error = "El nombre es requerido." });
                }

                var clientes = await _service.SearchByNameAsync(nombre, logId);
                await _logger.FinalizarLog(logId);
                return Ok(clientes);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en SearchByName: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpGet("search-email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var logId = IniciarLog();

            try
            {
                var cliente = await _service.GetByEmailAsync(email, logId);
                await _logger.FinalizarLog(logId);

                if (cliente == null) return NotFound(new { error = "Cliente no encontrado." });
                return Ok(cliente);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetByEmail: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
        {
            var logId = IniciarLog();

            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                await _logger.RegistrarErrorController(logId, $"Validación fallida: {string.Join(", ", errores)}");
                await _logger.FinalizarLog(logId);

                return BadRequest(new { errores });
            }

            try
            {
                var cliente = await _service.CreateAsync(dto, logId);
                await _logger.FinalizarLog(logId);
                return CreatedAtAction(nameof(GetByEmail), new { email = cliente.Email }, cliente);
            }
            catch (InvalidOperationException ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error de negocio: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error inesperado: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpPut("update-by-email/{email}")]
        public async Task<IActionResult> Update(string email, [FromBody] ClienteUpdateDto dto)
        {
            var logId = IniciarLog();

            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                await _logger.RegistrarErrorController(logId, $"Validación fallida: {string.Join(", ", errores)}");
                await _logger.FinalizarLog(logId);

                return BadRequest(new { errores });
            }

            try
            {
                var updatedCliente = await _service.UpdateAsync(email, dto, logId);
                await _logger.FinalizarLog(logId);

                if (updatedCliente == null) return NotFound();
                return Ok(updatedCliente);
            }
            catch (InvalidOperationException ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error de negocio: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error inesperado: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpDelete("delete-by-email/{email}")]
        public async Task<IActionResult> DeleteByEmail(string email)
        {
            var logId = IniciarLog();

            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    await _logger.RegistrarErrorController(logId, "Validación fallida: El email es requerido");
                    await _logger.FinalizarLog(logId);
                    return BadRequest(new { error = "El email es requerido." });
                }

                var deleted = await _service.DeleteByEmailAsync(email, logId);
                await _logger.FinalizarLog(logId);

                if (deleted == null)
                    return NotFound(new { error = "Cliente no encontrado." });

                if (deleted == false)
                    return BadRequest(new { error = "No se puede eliminar el cliente porque tiene pedidos asociados." });

                return Ok(new { message = "Cliente eliminado correctamente." });
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error al eliminar: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        // Método auxiliar SIMPLIFICADO - SIN Body
        private string IniciarLog()
        {
            try
            {
                var fecha = DateTime.Now;
                var entidad = HttpContext.Request.Path.Value ?? "";

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
                if (ip == "::1")
                {
                    ip = "127.0.0.1";
                }

                var metodo = HttpContext.Request.Method;
                var headers = string.Join("; ", HttpContext.Request.Headers
                    .Where(h => !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    .Select(h => $"{h.Key}: {h.Value}"));

                var statusCode = HttpContext.Response.StatusCode;

                return _logger.IniciarLog(fecha, entidad, ip, metodo, headers, statusCode);
            }
            catch (Exception ex)
            {
                return _logger.IniciarLog(
                    DateTime.Now,
                    HttpContext.Request.Path.Value ?? "Unknown",
                    "Error",
                    HttpContext.Request.Method,
                    $"Error capturando headers: {ex.Message}",
                    500
                );
            }
        }
    }
}
