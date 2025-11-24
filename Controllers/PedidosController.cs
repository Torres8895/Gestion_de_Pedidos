using Microsoft.AspNetCore.Mvc;
using Gestion_de_Pedidos.Service;
using static Gestion_de_Pedidos.Dto.PedidoDto;

namespace Gestion_de_Pedidos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly PedidosService _service;
        private readonly ContinuousLogger _logger;

        public PedidosController(PedidosService service, ContinuousLogger logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("cabeceras")]
        public async Task<IActionResult> GetAllCabeceras()
        {
            var logId = IniciarLog();

            try
            {
                var cabeceras = await _service.GetAllCabecerasAsync(logId);
                await _logger.FinalizarLog(logId);
                return Ok(cabeceras);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetAllCabeceras: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpGet("cabeceras/{numeroPedido}")]
        public async Task<IActionResult> GetCabeceraByNumero(string numeroPedido)
        {
            var logId = IniciarLog();

            try
            {
                var cabecera = await _service.GetCabeceraByNumeroAsync(numeroPedido, logId);
                await _logger.FinalizarLog(logId);

                if (cabecera == null) return NotFound(new { error = "Pedido no encontrado." });
                return Ok(cabecera);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetCabeceraByNumero: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpGet("{numeroPedido}/detalles")]
        public async Task<IActionResult> GetDetallesByPedido(string numeroPedido)
        {
            var logId = IniciarLog();

            try
            {
                var detalles = await _service.GetDetallesByPedidoAsync(numeroPedido, logId);
                await _logger.FinalizarLog(logId);

                if (detalles == null) return NotFound(new { error = "Pedido pendiente no encontrado." });
                return Ok(detalles);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error en GetDetallesByPedido: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] PedidoCabeceraCreateDto dto)
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
                var pedido = await _service.CreateAsync(dto, logId);
                await _logger.FinalizarLog(logId);
                return CreatedAtAction(nameof(GetCabeceraByNumero), new { numeroPedido = pedido.NumeroPedido }, pedido);
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

        [HttpPost("{numeroPedido}/detalles/create")]
        public async Task<IActionResult> CreateDetalle(string numeroPedido, [FromBody] PedidoDetalleCreateDto dto)
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
                var detalle = await _service.CreateDetalleAsync(numeroPedido, dto, logId);
                await _logger.FinalizarLog(logId);

                if (detalle == null) return NotFound(new { error = "Pedido no encontrado." });
                return CreatedAtAction(nameof(GetDetallesByPedido), new { numeroPedido = numeroPedido }, detalle);
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

        [HttpPut("cabeceras/{numeroPedido}")]
        public async Task<IActionResult> UpdateEstado(string numeroPedido, [FromBody] PedidoCabeceraUpdateDto dto)
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
                var pedido = await _service.UpdateEstadoAsync(numeroPedido, dto, logId);
                await _logger.FinalizarLog(logId);

                if (pedido == null) return NotFound(new { error = "Pedido no encontrado." });
                return Ok(pedido);
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

        [HttpPut("{numeroPedido}/detalles/{numeroDetalle}")]
        public async Task<IActionResult> UpdateDetalle(string numeroPedido, int numeroDetalle, [FromBody] PedidoDetalleUpdateDto dto)
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
                var detalle = await _service.UpdateDetalleAsync(numeroPedido, numeroDetalle, dto, logId);
                await _logger.FinalizarLog(logId);

                if (detalle == null) return NotFound(new { error = "Detalle no encontrado." });
                return Ok(detalle);
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

        [HttpDelete("{numeroPedido}/detalles/{numeroDetalle}")]
        public async Task<IActionResult> DeleteDetalle(string numeroPedido, int numeroDetalle)
        {
            var logId = IniciarLog();

            try
            {
                var resultado = await _service.DeleteDetalleAsync(numeroPedido, numeroDetalle, logId);
                await _logger.FinalizarLog(logId);

                if (resultado == null) return NotFound(new { error = "Detalle no encontrado." });
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error al eliminar detalle: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        [HttpDelete("cabeceras/{numeroPedido}")]
        public async Task<IActionResult> DeletePedido(string numeroPedido)
        {
            var logId = IniciarLog();

            try
            {
                var pedido = await _service.DeleteAsync(numeroPedido, logId);
                await _logger.FinalizarLog(logId);

                if (pedido == null) return NotFound(new { error = "Pedido no encontrado." });

                return Ok(new
                {
                    message = $"Pedido {numeroPedido} cancelado exitosamente.",
                    pedido = pedido
                });
            }
            catch (InvalidOperationException ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error de negocio: {ex.Message}");
                await _logger.FinalizarLog(logId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _logger.RegistrarErrorController(logId, $"Error al cancelar pedido: {ex.Message}");
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