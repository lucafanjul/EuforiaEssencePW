using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class FiltroAuditoriaViewModel : IValidatableObject
    {
        [StringLength(30)]
        [Display(Name = "Sección")]
        public string Seccion { get; set; } = "Todos";

        [DataType(DataType.Date)]
        public DateTime? Desde { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Hasta { get; set; }

        [StringLength(100)]
        public string? Entidad { get; set; }

        [StringLength(20)]
        [Display(Name = "Acción")]
        public string? Accion { get; set; }

        [StringLength(256)]
        public string? Usuario { get; set; }

        [ValidateNever]
        public List<string> Entidades { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if ((Desde.HasValue &&
                 (Desde.Value.Year < 1900 || Desde.Value.Year > 9998)) ||
                (Hasta.HasValue &&
                 (Hasta.Value.Year < 1900 || Hasta.Value.Year > 9998)))
            {
                yield return new ValidationResult(
                    "Ingresá fechas entre los años 1900 y 9998.");
            }

            if (Desde.HasValue && Hasta.HasValue &&
                Desde.Value.Date > Hasta.Value.Date)
            {
                yield return new ValidationResult(
                    "La fecha Desde no puede ser posterior a Hasta.");
            }
        }
    }
}