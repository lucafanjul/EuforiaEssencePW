using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class PerfilViewModel
    {
        [Required(ErrorMessage = "Ingresá tu nombre.")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá tu apellido.")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
        [StringLength(30)]
        public string? Telefono { get; set; }

        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;
    }
}