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

        public ProductosController(ProductoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var productos = await _service.GetAllAsync();
                return Ok(productos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { tipo = "Inesperado", mensaje = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var producto = await _service.GetByIdAsync(id);
                return Ok(producto);
            }
            catch (Exception ex)
            {
                string tipo = ex.Message.Contains("no encontrado") ? "Negocio" : "Inesperado";
                return StatusCode(tipo == "Negocio" ? 404 : 500, new { tipo, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductoCreateDto dto)
        {
            // Validación del DTO
            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    tipo = "Validacion",
                    mensaje = "Datos inválidos del modelo.",
                    errores
                });
            }

            try
            {
                var producto = await _service.CreateAsync(dto);

                // No incluir "Activo" si es true por defecto
                var result = new
                {
                    producto.Id,
                    producto.Nombre,
                    producto.Precio,
                    producto.Activo
                };

                return CreatedAtAction(nameof(GetById), new { id = producto.Id }, result);
            }
            catch (Exception ex)
            {
                // Diferenciar tipo de error
                string tipo = ex.Message.Contains("Ya existe") || ex.Message.Contains("No se puede") ? "Negocio/SQL" : "Inesperado";
                return BadRequest(new { tipo, mensaje = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    tipo = "Validacion",
                    mensaje = "Datos inválidos del modelo.",
                    errores
                });
            }

            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                string tipo = ex.Message.Contains("no encontrado") ? "Negocio" : "Negocio/SQL";
                return tipo == "Negocio"
                    ? NotFound(new { tipo, mensaje = ex.Message })
                    : BadRequest(new { tipo, mensaje = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                string tipo = ex.Message.Contains("no encontrado") ? "Negocio" : "Negocio/SQL";
                return tipo == "Negocio"
                    ? NotFound(new { tipo, mensaje = ex.Message })
                    : BadRequest(new { tipo, mensaje = ex.Message });
            }
        }
    }
}
