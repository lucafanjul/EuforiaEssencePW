using System.ComponentModel.DataAnnotations;

namespace EuforiaEssencePW.Models
{
    public class RegistroAuditoria
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(450)]
        public string? UsuarioId { get; set; }

        [Required]
        [StringLength(256)]
        public string UsuarioNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Entidad { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]     
        public string RegistroId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Accion { get; set; } = string.Empty;

        public string ValoresAnteriores { get; set; } = "{}";

        public string ValoresNuevos { get; set; } = "{}";
    }
}