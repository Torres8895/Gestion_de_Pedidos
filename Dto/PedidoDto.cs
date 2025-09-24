using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Pedidos.Dto
{
    public class PedidoDto
    {

        public class PedidoCabeceraReadDto
        {
            public string NumeroPedido { get; set; }
            public string NombreCliente { get; set; }
            public string EmailCliente { get; set; }
            public DateTime Fecha { get; set; }
            public string Estado { get; set; }
            public decimal Total { get; set; }
        }

        public class PedidoDetalleReadDto
        {
            public int numeroDetalle { get; set; }
            public string Producto { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
        }

        public class PedidoCabeceraCreateDto
        {
            [Required(ErrorMessage = "El Email del cliente es obligatorio.")]
            public string EmailCliente { get; set; }
            [Required(ErrorMessage = "El pedido debe tener al menos un Producto.")]
            [MinLength(1, ErrorMessage = "El pedido debe contener como minimo un Producto.")]
            public List<PedidoDetalleCreateDto> Detalles { get; set; }
        }
        public class PedidoDetalleCreateDto
        {
            [Required(ErrorMessage = "El Id del producto es obligatorio.")]
            public int ProductoId { get; set; }
            [Required(ErrorMessage = "La cantidad es obligatoria.")]
            [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
            public int Cantidad { get; set; }

        }

        public class PedidoCabeceraUpdateDto
        {
            [Required(ErrorMessage = "El estado del pedido es obligatorio.")]
            public string Estado { get; set; }
        }

        public class PedidoDetalleUpdateDto
        {
            [Required(ErrorMessage = "La cantidad es obligatoria.")]
            [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
            public int Cantidad { get; set; }
        }

        public class PedidoCabeceraDeleteDto
        {
            [Required(ErrorMessage = "El Nro del pedido es obligatorio.")]
            public string NumeroPedido { get; set; }
        }
        public class PedidoDetalleDeleteDto
        {
            [Required(ErrorMessage = "El Nro del detalle es obligatorio.")]
            public int numeroDetalle { get; set; }
        }


    }
}

   
