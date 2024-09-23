using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PuestosController : ControllerBase
    {
        private readonly DataContext contexto;
        public PuestosController(DataContext contexto)
        {
            this.contexto = contexto;
        }
        [HttpGet]
        public async Task<ActionResult<List<Puesto>>> Get()
        {
            try
            {
                var puestos = await contexto.Puesto.ToListAsync();
                return Ok(puestos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}