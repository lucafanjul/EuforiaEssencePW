using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class CambiarClaveViewModel
    {
        [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string Actual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Usá entre 8 y 100 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string Nueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Nueva),
            ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Repetir nueva contraseña")]
        public string Confirmacion { get; set; } = string.Empty;
    }
}