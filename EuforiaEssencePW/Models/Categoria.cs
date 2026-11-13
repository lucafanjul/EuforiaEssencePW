using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Categoria : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80)]
        [Display(Name = "Categoría")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        public ICollection<Perfume> Perfumes { get; set; }
            = new List<Perfume>();

        public bool PuedeEliminarse()
        {
            return !Perfumes.Any();
        }
    }
}