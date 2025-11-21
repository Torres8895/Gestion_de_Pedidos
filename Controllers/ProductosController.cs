using Microsoft.AspNetCore.Mvc;
using Gestion_de_Pedidos.Service;
using static Gestion_de_Pedidos.Dto.ProductoDto;

namespace Gestion_de_Pedidos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly ProductoService _service;
        private readonly ContinuousLogger _logger;

        public ProductosController(ProductoService service, ContinuousLogger logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logId = IniciarLog();

            try
            {
                var productos = await _service.GetAllAsync(logId);
                await _logger.FinalizarLog(logId);
                return Ok(productos);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetAll: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var logId = IniciarLog();

            try
            {
                var producto = await _service.GetByIdAsync(id, logId);
                await _logger.FinalizarLog(logId);

                if (producto == null) return NotFound();
                return Ok(producto);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetById: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductoCreateDto dto)
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
                var producto = await _service.CreateAsync(dto, logId);
                await _logger.FinalizarLog(logId);
                return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
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
                var producto = await _service.UpdateAsync(id, dto, logId);
                await _logger.FinalizarLog(logId);

                if (producto == null) return NotFound();
                return Ok(producto);
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var logId = IniciarLog();

            try
            {
                var deleted = await _service.DeleteAsync(id, logId);
                await _logger.FinalizarLog(logId);

                if (deleted == null)
                    return NotFound(new { error = "Producto no encontrado." });
                if (deleted == false)
                    return BadRequest(new { error = "No se puede eliminar el producto porque está asociado a pedidos activos." });
                return Ok(new { message = "Producto eliminado correctamente." });
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
                var fecha = DateTime.UtcNow;
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
                    DateTime.UtcNow,
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