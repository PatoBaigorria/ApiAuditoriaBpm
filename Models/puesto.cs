using System.ComponentModel.DataAnnotations;

namespace apiAuditoriaBPM.Models
{
    public class Puesto
    {
        [Key]
        public int IdPuesto { get; set; }

        [Required]
        [MaxLength(50)]
        public string Descripcion { get; set; }

    }
}