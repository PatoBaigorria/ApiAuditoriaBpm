using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace apiAuditoriaBPM.Models
{
    public class AuditoriaItemBPM
    {
        [Key]
        public int IdAuditoriaItemBPM { get; set; }

        [Required] 
        public int IdAuditoria{ get; set; }

        [Required]
        public int IdItemBPM { get; set; }

        [Required]
        public bool Estado { get; set; }

        [ForeignKey(nameof(IdAuditoria))]
        public Auditoria? Auditoria { get; set; }

        [ForeignKey(nameof(IdItemBPM))]
        public ItemBPM? ItemBPM { get; set; }	
     }
}
