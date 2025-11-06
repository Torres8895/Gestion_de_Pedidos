using System.ComponentModel.DataAnnotations;

namespace Gestion_de_Pedidos.Models
{
    public class SqlLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Entidad { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? SqlSentencia { get; set; }
        public string Resultado { get; set; } = string.Empty;
    }
}
