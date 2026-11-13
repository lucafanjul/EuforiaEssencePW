namespace EuforiaEssencePW.Models
{
    public class CampaniaPerfume : AuditableEntity
    {
        public int CampaniaId { get; set; }

        public Campania Campania { get; set; } = null!;

        public int PerfumeId { get; set; }

        public Perfume Perfume { get; set; } = null!;
    }
}