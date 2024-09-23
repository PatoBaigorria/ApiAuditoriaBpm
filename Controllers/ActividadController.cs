using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActividadesController : ControllerBase
    {
        private readonly DataContext contexto;
        public ActividadesController(DataContext contexto)
        {
            this.contexto = contexto;
        }
        [HttpGet]
        public async Task<ActionResult<List<Actividad>>> Get()
        {
            try
            {
                var actividades = await contexto.Actividad.ToListAsync();
                return Ok(actividades);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}