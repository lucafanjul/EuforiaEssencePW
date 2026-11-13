using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Usuario : IdentityUser
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Display(Name = "Foto de perfil")]
        public string? FotoPerfilUrl { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}