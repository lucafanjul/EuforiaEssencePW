using EuforiaEssencePW.Models;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class AuditoriaViewModel : FiltroAuditoriaViewModel
    {
        public int Total { get; set; }
        public int Altas { get; set; }
        public int Modificaciones { get; set; }
        public int Eliminaciones { get; set; }
        public int Errores { get; set; }
        public int Analizados { get; set; }
        public int TotalParaAnalisis { get; set; }
        public string? ResultadoIA { get; set; }
        public string? ErrorIA { get; set; }
        public int Pagina { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;

        public List<RegistroAuditoria> Registros { get; set; } = new();
    }
}