using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Direccion : AuditableEntity
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El destinatario es obligatorio.")]
        [StringLength(100)]
        public string Destinatario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La calle es obligatoria.")]
        [StringLength(120)]
        public string Calle { get; set; } = string.Empty;

        [Required(ErrorMessage = "La altura es obligatoria.")]
        [StringLength(10)]
        public string Altura { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Piso / departamento")]
        public string? PisoDepartamento { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        [StringLength(100)]
        public string Localidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(100)]
        public string Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código postal es obligatorio.")]
        [StringLength(12)]
        [Display(Name = "Código postal")]
        public string CodigoPostal { get; set; } = string.Empty;

        public Usuario? Usuario { get; set; }

        public ICollection<Pedido> Pedidos { get; set; }
            = new List<Pedido>();

        public bool PuedeEliminarse()
        {
            return !Pedidos.Any();
        }
    }
}