using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuforiaEssencePW.Models
{
    public class ComprobantePago : AuditableEntity
    {
        public Guid Id { get; set; }

        public int PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        [Required]
        public byte[] Contenido { get; set; } = Array.Empty<byte>();

        [Required]
        [StringLength(50)]
        public string TipoContenido { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Extension { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        [Column(TypeName = "decimal(18,2)")]
        public decimal ImporteEsperado { get; set; }

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        public DateTime? FechaRevision { get; set; }

        [StringLength(450)]
        public string? RevisadoPorId { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }
        [StringLength(500)]
        public string? MotivoApelacion { get; set; }
    }
}