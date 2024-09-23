using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OperariosController : ControllerBase
    {
        private readonly DataContext contexto;
        public OperariosController(DataContext contexto)
        {
            this.contexto = contexto;
        }
        [HttpGet]
        public async Task<ActionResult<List<Operario>>> Get()
        {
            try
            {
                var operarios = await contexto.Operario.ToListAsync();
                return Ok(operarios);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}