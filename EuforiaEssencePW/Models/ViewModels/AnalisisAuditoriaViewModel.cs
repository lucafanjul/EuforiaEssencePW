using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class AnalisisAuditoriaViewModel : FiltroAuditoriaViewModel
    {
        [ValidateNever]
        public int Total { get; set; }

        [ValidateNever]
        public int Analizados { get; set; }

        [ValidateNever]
        public string? Resultado { get; set; }

        [ValidateNever]
        public string? Error { get; set; }
    }
}