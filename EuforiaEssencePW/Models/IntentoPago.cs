using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuforiaEssencePW.Models
{
    public class IntentoPago : AuditableEntity
    {
        public Guid Id { get; set; }

        public int PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string Resultado { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Importe { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}