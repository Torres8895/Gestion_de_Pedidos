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
            var productos = await _service.GetAllAsync();
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var producto = await _service.GetByIdAsync(id);
            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductoCreateDto dto)
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
                var producto = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
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

                return BadRequest(new { errores });
            }

            try
            {
                var producto = await _service.UpdateAsync(id, dto);
                if (producto == null) return NotFound();
                return Ok(producto);
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

                if (deleted == null)
                    return NotFound(new { error = "Producto no encontrado." });

                if (deleted == false)
                    return BadRequest(new { error = "No se puede eliminar el producto porque está asociado a pedidos activos." });

                return Ok(new { message = "Producto eliminado correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno del servidor." });
            }
        }
    }
}
