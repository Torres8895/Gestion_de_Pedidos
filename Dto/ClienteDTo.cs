using System.ComponentModel.DataAnnotations;

namespace Gestion_de_Pedidos.Dto
{
    public class ClienteDto
    {
        public class ClienteReadDto
        {
            public string Nombre { get; set; }
            public string Email { get; set; }
        }

        public class ClienteCreateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
            public string Nombre { get; set; }

            [Required(ErrorMessage = "El email es obligatorio.")]
            [StringLength(50, ErrorMessage = "El email no puede superar los 50 caracteres.")]
            [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
            public string Email { get; set; }

        }

        public class ClienteUpdateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
            public string Nombre { get; set; }

            [Required(ErrorMessage = "El email es obligatorio.")]
            [StringLength(50, ErrorMessage = "El email no puede superar los 50 caracteres.")]
            [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
            public string Email { get; set; }
        }
    }
}