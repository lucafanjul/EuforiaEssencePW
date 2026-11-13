using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class DireccionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingrese el destinatario.")]
        [StringLength(100)]
        public string Destinatario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la calle.")]
        [StringLength(120)]
        public string Calle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la altura.")]
        [StringLength(10)]
        public string Altura { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Piso / departamento")]
        public string? PisoDepartamento { get; set; }

        [Required(ErrorMessage = "Ingrese la localidad.")]
        [StringLength(100)]
        public string Localidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la provincia.")]
        [StringLength(100)]
        public string Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese el código postal.")]
        [StringLength(12)]
        [Display(Name = "Código postal")]
        public string CodigoPostal { get; set; } = string.Empty;
    }
}