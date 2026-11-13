using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuforiaEssencePW.Models
{
    public class DetallePedido : AuditableEntity
    {
        public int Id { get; set; }

        public int PedidoId { get; set; }

        public int PresentacionId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public int Cantidad { get; set; }

        [Range(typeof(decimal), "0.01", "99999999",
            ErrorMessage = "El precio debe ser mayor que cero.")]
        [Display(Name = "Precio unitario")]
        public decimal PrecioUnitario { get; set; }

        // Conservamos los datos del producto al comprar.
        [Required]
        [StringLength(120)]
        [Display(Name = "Perfume")]
        public string NombrePerfume { get; set; } = string.Empty;

        [Range(1, 2000)]
        public int Mililitros { get; set; }

        public Pedido? Pedido { get; set; }

        public Presentacion? Presentacion { get; set; }

        [NotMapped]
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}