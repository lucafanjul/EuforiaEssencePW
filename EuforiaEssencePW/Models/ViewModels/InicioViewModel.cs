using EuforiaEssencePW.Models;

namespace EuforiaEssencePW.Models.ViewModels
{
    public class InicioViewModel
    {
        public Campania? Campania { get; set; }

        public List<Perfume> Seleccion { get; set; } = new();

        public List<Perfume> Novedades { get; set; } = new();

        public List<Categoria> Categorias { get; set; } = new();
    }
}