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

        [HttpGet("estado-false")]
        public async Task<ActionResult<List<object>>> GetAuditoriaItemsWithFalseEstado([FromForm] int legajo)
        {
            var operario = await contexto.Operario
                .FirstOrDefaultAsync(o => o.Legajo == legajo);

            if (operario == null)
            {
                return NotFound("Operario no encontrado.");
            }

            // Obtener los items con estado false, agrupándolos por descripción
            var items = await contexto.AuditoriaItemBPM
                                      .Where(a => a.Estado == false)
                                      .Include(a => a.Auditoria)
                                      .ThenInclude(a => a.Operario)
                                      .Where(a => a.Auditoria.Operario.Legajo == legajo)
                                      .Include(a => a.ItemBPM)
                                      .GroupBy(a => a.ItemBPM.Descripcion)
                                      .Select(g => new
                                      {
                                          Descripcion = g.Key, // Descripción del item
                                          Count = g.Count(), // Contar la cantidad de veces que se repite
                                          Comentario = g.FirstOrDefault().Auditoria.Comentario != null ? g.FirstOrDefault().Auditoria.Comentario : "",
                                      })
                                      .ToListAsync();

            if (items == null || !items.Any())
            {
                return NotFound("No se encontraron items con estado false.");
            }

            // Retornar los items encontrados y sus cantidades
            return Ok(items);
        }

    }
}