using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuditoriasItemBPMController : ControllerBase
    {
        private readonly DataContext contexto;

        public AuditoriasItemBPMController(DataContext contexto)
        {
            this.contexto = contexto;
        }
        [HttpGet]
        public async Task<ActionResult<List<AuditoriaItemBPM>>> GetAuditoriaItems()
        {
            var items = await contexto.AuditoriaItemBPM
                                      .Include(a => a.Auditoria)
                                      .Include(a => a.ItemBPM)
                                      .Select(item => new AuditoriaItemBPM
                                      {
                                          IdAuditoriaItemBPM = item.IdAuditoriaItemBPM,
                                          IdAuditoria = item.IdAuditoria,
                                          IdItemBPM = item.IdItemBPM,
                                          Estado = item.Estado,
                                          Comentario = item.Auditoria.Comentario != null ? item.Auditoria.Comentario : "",
                                          Aplica = item.Aplica
                                      })
                                      .ToListAsync();

            return items;
        }



    }
}