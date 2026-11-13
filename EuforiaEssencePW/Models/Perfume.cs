using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Perfume : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(2000)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Notas olfativas")]
        public string? NotasOlfativas { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione una marca.")]
        [Display(Name = "Marca")]
        public int MarcaId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione una categoría.")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        public Marca? Marca { get; set; }

        public Categoria? Categoria { get; set; }

        [StringLength(300)]
        [Display(Name = "Imagen principal")]
        public string? ImagenUrl { get; set; }

        public ICollection<Presentacion> Presentaciones { get; set; }
            = new List<Presentacion>();

        public bool PuedeEliminarse()
        {
            return !Presentaciones.Any();
        }
    }
}