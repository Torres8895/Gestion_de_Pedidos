using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Pedidos.Dto
{
    public class ProductoDto
    {

        public class ProductoReadDto
        {
            [Key]
            public int Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
        }

        public class ProductoCreateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
            public string Nombre { get; set; }

            [Required(ErrorMessage = "El precio es obligatorio.")]
            [Column(TypeName = "decimal(10,2)")]
            [Range(0.01, 99999999.99, ErrorMessage = "El precio esta fuera de rango.")]
            public decimal Precio { get; set; }
        }

        public class ProductoUpdateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
            public string Nombre { get; set; }

            [Required(ErrorMessage = "El precio es obligatorio.")]
            [Column(TypeName = "decimal(10,2)")]
            [Range(0.01, 99999999.99, ErrorMessage = "El precio esta fuera de rango.")]
            public decimal Precio { get; set; }
            
        }
    }
}
