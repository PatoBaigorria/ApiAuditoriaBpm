using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuditoriasController : ControllerBase
    {
        private readonly DataContext contexto;

        public AuditoriasController(DataContext contexto)
        {
            this.contexto = contexto;
        }

        // POST: Auditorias/alta
        [HttpPost("alta")]
        public async Task<IActionResult> DarDeAlta([FromBody] Auditoria auditoria)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await contexto.Auditoria.AddAsync(auditoria);
                await contexto.SaveChangesAsync();
                return Ok(new
                {
                    message = "Auditoría creada correctamente",
                    auditoria 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
