using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class Presentacion : AuditableEntity
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione un perfume.")]
        [Display(Name = "Perfume")]
        public int PerfumeId { get; set; }

        [Range(1, 2000,
            ErrorMessage = "Ingrese un volumen entre 1 y 2000 ml.")]
        [Display(Name = "Contenido (ml)")]
        public int Mililitros { get; set; }

        [Range(0.01, 99999999,
            ErrorMessage = "El precio debe ser mayor que cero y no superar 99.999.999.")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        public bool Activo { get; set; } = true;

        public Perfume? Perfume { get; set; }
    }
}