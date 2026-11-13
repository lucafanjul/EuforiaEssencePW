using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class ConfirmarCompraViewModel
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione una dirección.")]
        public int DireccionId { get; set; }

        public Guid Token { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [ValidateNever]
        public CarritoViewModel Carrito { get; set; } = new();

        [ValidateNever]
        public List<Direccion> Direcciones { get; set; } = new();
    }
}