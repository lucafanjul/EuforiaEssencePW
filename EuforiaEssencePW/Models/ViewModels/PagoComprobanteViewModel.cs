using EuforiaEssencePW.Models;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class PagoComprobanteViewModel
    {
        public Pedido Pedido { get; set; } = null!;

        public Guid Token { get; set; }

        public List<ComprobanteResumenViewModel> Historial { get; set; }
            = new();
    }

    public class ComprobanteResumenViewModel
    {
        public Guid Id { get; set; }

        public int PedidoId { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public decimal ImporteEsperado { get; set; }

        public DateTime FechaEnvio { get; set; }

        public DateTime? FechaRevision { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string EstadoPedido { get; set; } = string.Empty;

        public string? MotivoRechazo { get; set; }
        public string? MotivoApelacion { get; set; }

        public bool PuedeRevisarse { get; set; }
    }
}