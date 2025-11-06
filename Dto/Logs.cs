namespace Gestion_de_Pedidos.Dto
{
    public class Logs
    {
        public class NegocioLogDto
        {
            public string Entidad { get; set; } = string.Empty;  // Ej: "Producto"
            public string Accion { get; set; } = string.Empty;   // Ej: "Crear", "Actualizar"
            public string Mensaje { get; set; } = string.Empty;  // Mensaje del error o alerta de negocio
        }

        public class SqlLogDto
        {
            public string Entidad { get; set; } = string.Empty;       // Ej: "Producto"
            public string Accion { get; set; } = string.Empty;        // Ej: "Insertar", "Actualizar"
            public string Mensaje { get; set; } = string.Empty;       // Mensaje de excepción
            public string? SqlSentencia { get; set; }                 // Sentencia SQL o LINQ generada
        }
    }
}
