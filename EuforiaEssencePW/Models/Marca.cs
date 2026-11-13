using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Marca : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80)]
        [Display(Name = "Marca")]
        public string Nombre { get; set; } = string.Empty;

        public ICollection<Perfume> Perfumes { get; set; }
            = new List<Perfume>();

        public bool PuedeEliminarse()
        {
            return !Perfumes.Any();
        }
    }
}