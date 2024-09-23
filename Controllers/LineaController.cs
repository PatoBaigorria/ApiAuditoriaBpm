using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LineasController : ControllerBase
    {
        private readonly DataContext contexto;
        public LineasController(DataContext contexto)
        {
            this.contexto = contexto;
        }
        [HttpGet]
        public async Task<ActionResult<List<Linea>>> Get()
        {
            try
            {
                var lineas = await contexto.Linea.ToListAsync();
                return Ok(lineas);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}