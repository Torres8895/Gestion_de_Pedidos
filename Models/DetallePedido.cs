using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Pedidos.Models
{
    public class DetallePedido
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(4)]
        [Display(Name = "Nº Detalle")]
        public int NumeroDetalle { get; set; } = 1;

        [Required]
        [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
        public int Cantidad { get; set; }

        //------------Datos de navegación----------------//  
        public int CabeceraPedidoId { get; set; }
        [ForeignKey("CabeceraPedidoId")]
          public CabeceraPedido CabeceraPedido { get; set; }
        
        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
          public Producto Producto { get; set; }
    } 
}