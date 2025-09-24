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

        public PedidosController(PedidosService service)
        {
            _service = service;
        }

        // Obtener todas las cabeceras de pedidos
        [HttpGet("cabeceras")]
        public async Task<IActionResult> GetAllCabeceras()
        {
            var cabeceras = await _service.GetAllCabecerasAsync();
            return Ok(cabeceras);
        }

        // Obtener cabecera por número de pedido
        [HttpGet("cabeceras/{numeroPedido}")]
        public async Task<IActionResult> GetCabeceraByNumero(string numeroPedido)
        {
            var cabecera = await _service.GetCabeceraByNumeroAsync(numeroPedido);
            if (cabecera == null) return NotFound(new { error = "Pedido no encontrado." });
            return Ok(cabecera);
        }

        // Obtener todos los detalles de un pedido específico
        [HttpGet("{numeroPedido}/detalles")]
        public async Task<IActionResult> GetDetallesByPedido(string numeroPedido)
        {
            var detalles = await _service.GetDetallesByPedidoAsync(numeroPedido);
            return Ok(detalles);
        }

        // Crear un nuevo pedido completo (cabecera + detalles)
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] PedidoCabeceraCreateDto dto)
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
                var pedido = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetCabeceraByNumero), new { numeroPedido = pedido.NumeroPedido }, pedido);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Agregar un detalle a un pedido existente
        [HttpPost("{numeroPedido}/detalles/create")]
        public async Task<IActionResult> CreateDetalle(string numeroPedido, [FromBody] PedidoDetalleCreateDto dto)
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
                var detalle = await _service.CreateDetalleAsync(numeroPedido, dto);
                if (detalle == null) return NotFound(new { error = "Pedido no encontrado." });
                return CreatedAtAction(nameof(GetDetallesByPedido), new { numeroPedido = numeroPedido }, detalle);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Actualizar el estado de un pedido
        [HttpPut("cabeceras/{numeroPedido}")]
        public async Task<IActionResult> UpdateEstado(string numeroPedido, [FromBody] PedidoCabeceraUpdateDto dto)
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
                var pedido = await _service.UpdateEstadoAsync(numeroPedido, dto);
                if (pedido == null) return NotFound(new { error = "Pedido no encontrado." });
                return Ok(pedido);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Actualizar la cantidad de un detalle específico
        [HttpPut("{numeroPedido}/detalles/{numeroDetalle}")]
        public async Task<IActionResult> UpdateDetalle(string numeroPedido, int numeroDetalle, [FromBody] PedidoDetalleUpdateDto dto)
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
                var detalle = await _service.UpdateDetalleAsync(numeroPedido, numeroDetalle, dto);
                if (detalle == null) return NotFound(new { error = "Detalle no encontrado." });
                return Ok(detalle);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Eliminar un detalle específico de un pedido
        [HttpDelete("{numeroPedido}/detalles/{numeroDetalle}")]
        public async Task<IActionResult> DeleteDetalle(string numeroPedido, int numeroDetalle)
        {
            try
            {
                var resultado = await _service.DeleteDetalleAsync(numeroPedido, numeroDetalle);
                if (resultado == null) return NotFound(new { error = "Detalle no encontrado." });

                // El servicio ahora retorna un objeto con diferentes tipos de respuesta
                return Ok(resultado);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }

        // Eliminar un pedido completo (soft delete - marca como cancelado)
        [HttpDelete("cabeceras/{numeroPedido}")]
        public async Task<IActionResult> DeletePedido(string numeroPedido)
        {
            try
            {
                var pedido = await _service.DeleteAsync(numeroPedido);
                if (pedido == null) return NotFound(new { error = "Pedido no encontrado." });

                return Ok(new
                {
                    message = $"Pedido {numeroPedido} cancelado exitosamente.",
                    pedido = pedido
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }
    }
}