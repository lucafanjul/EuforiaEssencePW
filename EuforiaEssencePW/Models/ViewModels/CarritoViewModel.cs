namespace EuforiaEssencePW.Models.ViewModels
{
    public class CarritoViewModel
    {
        public List<CarritoLineaViewModel> Lineas { get; set; } = new();

        public decimal Total => Lineas.Sum(l => l.Subtotal);

        public bool PuedeComprar =>
            Lineas.Count > 0 && Lineas.All(l => l.Disponible);
    }

    public class CarritoLineaViewModel
    {
        public int PresentacionId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? ImagenUrl { get; set; }

        public int Mililitros { get; set; }

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public bool Disponible { get; set; }

        public decimal Subtotal => Precio * Cantidad;
    }
}