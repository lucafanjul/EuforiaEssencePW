using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Campania : AuditableEntity, IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingresá un título.")]
        [StringLength(100)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá una descripción.")]
        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de inicio")]
        public DateTime Desde { get; set; } =
            DateTime.UtcNow.AddHours(-3).Date;

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de finalización")]
        public DateTime Hasta { get; set; } =
            DateTime.UtcNow.AddHours(-3).Date.AddDays(30);

        [Range(0, 999)]
        [Display(Name = "Prioridad")]
        public int Prioridad { get; set; }

        public bool Activa { get; set; } = true;

        public ICollection<CampaniaPerfume> Productos { get; set; }
            = new List<CampaniaPerfume>();

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Hasta.Date < Desde.Date)
            {
                yield return new ValidationResult(
                    "La finalización no puede ser anterior al inicio.",
                    new[] { nameof(Hasta) });
            }
        }
    }
}