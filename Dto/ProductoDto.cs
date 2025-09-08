namespace Gestion_de_Pedidos.Dto
{
    public class ProductoDto
    {
        public class ProductoCreateDto
        {
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public bool Activo { get; set; } = true;
        }

        public class ProductoUpdateDto
        {
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public bool Activo { get; set; }
        }
    }
}
