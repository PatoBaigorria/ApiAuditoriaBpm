using System.Numerics;
using System.Security.Claims;
using apiAuditoriaBPM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace apiAuditoriaBPM.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuditoriasController : ControllerBase
    {
        private readonly DataContext contexto;

        private readonly IConfiguration config;

        public AuditoriasController(DataContext contexto, IConfiguration config)
        {
            this.contexto = contexto;
            this.config = config;
        }


        // GET: Cantidad Auditorias realizadas por Supervisor
        [HttpGet("cantidad-auditorias-mes-a-mes")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

                        // Contar las auditorías en base a los ítems que tengan estado (1: no ok, 2: ok, 3: n/a)
                        var auditoriasConEstadoNoOk = 0;
                        var auditoriasConEstadoOk = 0;

                        foreach (var auditoria in auditoriasMes)
                        {
                            // Verificar si al menos un ítem tiene estado NOOK (2)
                            var tieneEstadoNoOK = await contexto.AuditoriaItemBPM
                                .AnyAsync(ai => ai.IdAuditoria == auditoria.IdAuditoria && ai.Estado == EstadoEnum.NOOK);

                            // Si no tiene ítems en estado NOOk, consideramos que todos los ítems son OK o n/a
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
        [HttpPost("enviar-notificacion-auditoria")]
        public async Task<IActionResult> EnviarNotificacionAuditoria([FromForm] int idAuditoria)
        {
            try
            {
                var auditoria = await contexto.Auditoria
                    .Include(a => a.Operario)
                    .Include(a => a.Supervisor)
                    .FirstOrDefaultAsync(a => a.IdAuditoria == idAuditoria);

                // Verificar que la auditoría y los datos relevantes no son null

                if (auditoria == null)
                {
                    return NotFound("No se encontró la auditoría especificada.");
                }

                var operario = auditoria.Operario;
                Console.WriteLine("Operario: " + operario?.Nombre);
                if (operario == null)
                {
                    Console.WriteLine("Operario no encontrado en la auditoría.");
                    return BadRequest("El operario no está asociado con esta auditoría.");
                }

                Console.WriteLine("Operario: " + operario?.Email);

                if (string.IsNullOrEmpty(operario.Nombre) || string.IsNullOrEmpty(operario.Email))
                {
                    Console.WriteLine("El operario o el correo electrónico son null.");
                    return BadRequest("El operario no tiene un correo electrónico o nombre registrado.");
                }

                var supervisor = auditoria.Supervisor;
                if (supervisor == null || string.IsNullOrEmpty(supervisor.Nombre))
                {
                    return BadRequest("El supervisor no tiene un nombre registrado.");
                }

                // Verificar que la fecha de la auditoría no sea null
                if (auditoria.Fecha == null)
                {
                    return BadRequest("La fecha de la auditoría es inválida.");
                }

                var auditoriaItems = await contexto.AuditoriaItemBPM.Include(i => i.ItemBPM).Where(ai => ai.IdAuditoria == idAuditoria).ToListAsync();
                if (auditoriaItems == null || !auditoriaItems.Any())
                {
                    return BadRequest("No hay ítems asociados a esta auditoría.");
                }

                // Construcción del cuerpo del correo con validación
                var cuerpoCorreo = $@"
                    <h1>Hola {operario.Nombre},</h1>
                    <p>Se ha completado una auditoría con los siguientes detalles:</p>
                    <ul>
                        <li><strong>ID Auditoría:</strong> {auditoria.IdAuditoria}</li>
                        <li><strong>Fecha:</strong> {auditoria.Fecha.ToString("dd/MM/yyyy")}</li>
                        <li><strong>Supervisor:</strong> {supervisor.Nombre}</li>
                        <li><strong>Total de Ítems:</strong> {auditoriaItems.Count}</li>
                    </ul>
                    <h2>Resumen de Ítems:</h2>
                    <table>
                        <tr><th>Ítem</th><th>Estado</th><th>Comentario</th></tr>";

                // Agregar los ítems de auditoría al cuerpo del correo
                foreach (var item in auditoriaItems)
                {
                    cuerpoCorreo += $@"
                        <tr>
                            <td>{item.ItemBPM.Descripcion}</td>
                            <td>{item.Estado}</td>
                            <td>{item.Comentario}</td>
                        </tr>";
                }

                cuerpoCorreo += "</table>";
                Console.WriteLine("Cuerpo del correo generado:");
                Console.WriteLine(cuerpoCorreo);

                // Crear el mensaje del correo
                var message = new MimeMessage();
                message.To.Add(new MailboxAddress(operario.Nombre, operario.Email));
                message.From.Add(new MailboxAddress("Sistema de Auditorías", config["SMTPUser"]));
                message.Subject = $"Notificación de Auditoría #{auditoria.IdAuditoria}";
                message.Body = new TextPart("html") { Text = cuerpoCorreo };

                // Enviar el correo
                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync("sandbox.smtp.mailtrap.io", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(config["SMTPUser"], config["SMTPPass"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return Ok("Se ha enviado la notificación de la auditoría correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el correo: {ex.Message}");
                Console.WriteLine($"Detalles: {ex.StackTrace}");
                return StatusCode(500, $"Error al enviar el correo: {ex.Message}. Detalles: {ex.StackTrace}");
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
        public class AltaAuditoriaRequest
        {
            public int IdOperario { get; set; }
            public int IdSupervisor { get; set; }
            public int IdActividad { get; set; }
            public int IdLinea { get; set; }
            public string? Comentario { get; set; }
            public List<ItemAuditoriaRequest> Items { get; set; } = new();
        }

        public class ItemAuditoriaRequest
        {
            public int IdItemBPM { get; set; }
            public string Estado { get; set; }  // Enum o string (por ejemplo, "OK", "NO_OK", "N/A")
            public string? Comentario { get; set; }
        }


        // POST: Auditorias/alta
        [HttpPost("alta-auditoria-completa")]
        public async Task<IActionResult> DarDeAltaAuditoriaCompleta([FromBody] AltaAuditoriaRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var nuevaAuditoria = new Auditoria
                {
                    IdSupervisor = request.IdSupervisor,
                    IdOperario = request.IdOperario,
                    IdActividad = request.IdActividad,
                    IdLinea = request.IdLinea,
                    Fecha = DateOnly.FromDateTime(DateTime.Now),
                    Comentario = request.Comentario
                };

                await contexto.Auditoria.AddAsync(nuevaAuditoria);
                await contexto.SaveChangesAsync();

                foreach (var item in request.Items)
                {
                    var nuevoAuditoriaItem = new AuditoriaItemBPM
                    {
                        IdAuditoria = nuevaAuditoria.IdAuditoria,
                        IdItemBPM = item.IdItemBPM,
                        Estado = Enum.Parse<EstadoEnum>(item.Estado),
                        Comentario = item.Comentario
                    };
                    await contexto.AuditoriaItemBPM.AddAsync(nuevoAuditoriaItem);
                }

                await contexto.SaveChangesAsync();
                return CreatedAtAction(nameof(DarDeAltaAuditoriaCompleta), new { id = nuevaAuditoria.IdAuditoria }, new { message = "Auditoría completa creada correctamente", auditoria = nuevaAuditoria });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Error interno del servidor: {errorMessage}");
            }


        }
    }
}
