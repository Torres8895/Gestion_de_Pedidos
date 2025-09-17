
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Pedidos.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(50)]
        public string Email { get; set; }

        public bool? Activo { get; set; }
    }
}