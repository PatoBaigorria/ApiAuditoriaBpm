using System.Numerics;
using System.Security.Claims;
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


        // GET: Cantidad Auditorias realizadas por Supervisor
        [HttpGet("cantidad-auditorias-mes-a-mes")]
        public async Task<ActionResult<Dictionary<string, object>>> GetAuditoriasMesAMes([FromQuery] int anioInicio, [FromQuery] int anioFin)
        {
            try
            {
                // Obtener el legajo del usuario autenticado
                var legajo = User.FindFirstValue("Legajo");

                if (legajo == null)
                {
                    return Unauthorized("Legajo no encontrado en el token.");
                }

                var legajoInt = int.Parse(legajo);

                // Buscar el supervisor por su legajo
                var supervisor = await contexto.Supervisor
                    .Where(s => s.Legajo == legajoInt)
                    .FirstOrDefaultAsync();

                // Verificar si el supervisor existe
                if (supervisor == null)
                {
                    return NotFound("Supervisor no encontrado.");
                }

                // Diccionario para almacenar los resultados por mes y totales
                var auditoriasPorMes = new Dictionary<string, object>();
                int totalAnual = 0;
                int totalConEstadoNoOkAnual = 0;
                int totalConEstadoOkAnual = 0;

                // Iterar por cada año y mes en el rango
                for (int anio = anioInicio; anio <= anioFin; anio++)
                {
                    for (int mes = 1; mes <= 12; mes++)
                    {
                        // Obtener todas las auditorías del supervisor para el mes y año actuales
                        var auditoriasMes = await contexto.Auditoria
                            .Where(a => a.IdSupervisor == supervisor.IdSupervisor &&
                                        a.Fecha.Year == anio &&
                                        a.Fecha.Month == mes)
                            .ToListAsync();

                        // Contar las auditorías en base a los ítems que tengan estado false o true/null
                        var auditoriasConEstadoNoOk = 0;
                        var auditoriasConEstadoOk = 0;

                        foreach (var auditoria in auditoriasMes)
                        {
                            // Verificar si al menos un ítem tiene estado false (0)
                            var tieneEstadoNoOK = await contexto.AuditoriaItemBPM
                                .AnyAsync(ai => ai.IdAuditoria == auditoria.IdAuditoria && ai.Estado == false);

                            // Si no tiene ítems en estado NOOk, consideramos que todos los ítems son OK o null
                            if (tieneEstadoNoOK)
                            {
                                auditoriasConEstadoNoOk++;
                            }
                            else
                            {
                                // Considerar tanto estado OK como null para las auditorías con estado OK
                                auditoriasConEstadoOk++;
                            }
                        }

                        // Formato de clave "YYYY-MM"
                        string clave = $"{anio}-{mes:D2}";

                        // Almacenar los resultados por mes
                        auditoriasPorMes[clave] = new
                        {
                            Total = auditoriasMes.Count,
                            ConEstadoNoOk = auditoriasConEstadoNoOk,
                            ConEstadoOK = auditoriasConEstadoOk
                        };

                        // Sumar los resultados al total anual
                        totalAnual += auditoriasMes.Count;
                        totalConEstadoNoOkAnual += auditoriasConEstadoNoOk;
                        totalConEstadoOkAnual += auditoriasConEstadoOk;
                    }
                }

                // Añadir los totales anuales al resultado final
                auditoriasPorMes["TotalesAnuales"] = new
                {
                    TotalAnual = totalAnual,
                    TotalConEstadoNoOkAnual = totalConEstadoNoOkAnual,
                    TotalConEstadoOkAnual = totalConEstadoOkAnual
                };

                return Ok(auditoriasPorMes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: Operario Sin Auditoría
        [HttpGet("auditorias-operario")]
        public IActionResult GetOperariosSinAuditoria()
        {
            // Asumiendo que tienes un contexto de base de datos llamado 'dbContext'

            var operariosSinAuditoria = contexto.Operario
                .Where(o => !contexto.Auditoria.Any(a => a.IdOperario == o.IdOperario))
                .ToList();

            if (!operariosSinAuditoria.Any())
            {
                return NotFound("Todos los operarios tienen auditorías realizadas.");
            }

            return Ok(operariosSinAuditoria);
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
                    message = "Auditoría creada correctamente", auditoria
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
