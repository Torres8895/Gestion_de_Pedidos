namespace Gestion_de_Pedidos.Models
{
    public class LogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Fecha { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Metodo { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? DatosServicio { get; set; }
        public string? SqlQuery { get; set; }
        public string? ResultadoServicio { get; set; }
        public string? ErrorController { get; set; } // NUEVO: Para errores en el controller
    }
}
