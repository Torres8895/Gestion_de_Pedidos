using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Pedidos.Models
{
    public class CabeceraPedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(4)]
        [Display(Name = "Número de Pedido")]
        public string NumeroPedido { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha del Pedido")]
        [DataType(DataType.DateTime)]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Estado del Pedido")]
        public string? Estado { get; set; }

        // Propiedades de navegación para mantienes relación con Cliente
        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        // Relación con detalles del pedido
        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    }

}