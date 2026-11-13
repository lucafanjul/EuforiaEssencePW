using EuforiaEssencePW.Models;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class PagoSimuladoViewModel
    {
        public Pedido Pedido { get; set; } = null!;

        public Guid Token { get; set; }
    }
}