using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuforiaEssencePW.Models
{
    public class Pedido : AuditableEntity
    {
        public Guid? TokenConfirmacion { get; set; }

        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Display(Name = "Dirección")]
        public int DireccionId { get; set; }

        [Display(Name = "Fecha del pedido")]
        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        [Required]
        [StringLength(30)]
        [Display(Name = "Estado del pago")]
        public string EstadoPago { get; set; } = "Pendiente";

        [Display(Name = "Pago validado por administrador")]
        public bool PagoValidadoPorAdmin { get; set; }
        public bool StockLiberado { get; set; }
        [StringLength(60)]
        [Display(Name = "Referencia del pago")]
        public string? ReferenciaPago { get; set; }

        [Display(Name = "Fecha del pago")]
        public DateTime? FechaPago { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [StringLength(600)]
        [Display(Name = "Dirección de entrega")]
        public string DireccionEntrega { get; set; } = string.Empty;

        public Usuario? Usuario { get; set; }

        public Direccion? Direccion { get; set; }

        public ICollection<DetallePedido> Detalles { get; set; }
            = new List<DetallePedido>();

        [NotMapped]
        public decimal Total => Detalles.Sum(d => d.Subtotal);

        public bool PuedePagarse()
        {
            return Estado == "Pendiente" &&
                !PagoValidadoPorAdmin &&
                (EstadoPago == "Pendiente" ||
                 EstadoPago == "Rechazado" ||
                 EstadoPago == "No requerido" ||
                 EstadoPago == "Aprobado");
        }

        public bool PuedeCambiarA(string nuevoEstado)
        {
            if (EstadoPago != "Aprobado" || !PagoValidadoPorAdmin)
                return false;

            return (Estado, nuevoEstado) switch
            {
                ("Pendiente", "En preparación") => true,
                ("En preparación", "Enviado") => true,
                ("Enviado", "Entregado") => true,
                _ => false
            };
        }

        public bool PuedeCancelarse()
        {
            return Estado == "Pendiente";
        }
    }
}