using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Security.Claims;
using System.Text.Json;

namespace EuforiaEssencePW.Services
{
    public class RegistroErroresService
    {
        public const string MarcaSolicitud =
            "Euforia.ErrorAuditoriaRegistrado";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<RegistroErroresService> _logger;

        public RegistroErroresService(
            IServiceScopeFactory scopeFactory,
            IHttpContextAccessor http,
            ILogger<RegistroErroresService> logger)
        {
            _scopeFactory = scopeFactory;
            _http = http;
            _logger = logger;
        }

        public async Task RegistrarAsync(
            string codigo,
            Exception? error = null,
            int? estadoHttp = null)
        {
            var http = _http.HttpContext;

            if (http == null ||
                http.Items.ContainsKey(MarcaSolicitud))
            {
                return;
            }

            var accion = http.GetEndpoint()?.Metadata
                .GetMetadata<ControllerActionDescriptor>();

            var pagina = accion == null
                ? "Solicitud HTTP"
                : $"{accion.ControllerName}/{accion.ActionName}";

            var usuarioNombre = http.User.Identity?.IsAuthenticated == true
                ? http.User.Identity.Name ?? "Usuario"
                : "Anónimo";

            var identificador = http.TraceIdentifier;

            var valores = new
            {
                Codigo = Recortar(codigo, 80),
                Pagina = Recortar(pagina, 150),
                Metodo = Recortar(http.Request.Method, 15),
                EstadoHttp = estadoHttp ?? http.Response.StatusCode,
                TipoExcepcion = error?.GetBaseException().GetType().Name,
                Descripcion = error == null
                    ? "Solicitud HTTP no completada correctamente."
                    : "La operación encontró un error."
            };

            try
            {
                // Un contexto nuevo permite registrar después de que
                // otra operación haya fallado o revertido su transacción.
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var contexto = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                contexto.RegistrosAuditoria.Add(new RegistroAuditoria
                {
                    Fecha = DateTime.UtcNow,
                    UsuarioId = http.User.FindFirstValue(
                        ClaimTypes.NameIdentifier),
                    UsuarioNombre = Recortar(usuarioNombre, 256),
                    Entidad = "Sistema",
                    RegistroId = Recortar(identificador, 100),
                    Accion = "Error",
                    ValoresAnteriores = "{}",
                    ValoresNuevos = JsonSerializer.Serialize(valores)
                });

                using var limite = new CancellationTokenSource(
                    TimeSpan.FromSeconds(3));

                await contexto.SaveChangesAsync(limite.Token);

                http.Items[MarcaSolicitud] = true;
            }
            catch (Exception falloRegistro)
            {
                // No reemplazamos el error original por un error del historial.
                _logger.LogError(
                    falloRegistro,
                    "No se pudo guardar el registro de error {Codigo}.",
                    codigo);
            }
        }

        private static string Recortar(string texto, int maximo)
        {
            return texto.Length <= maximo
                ? texto
                : texto[..maximo];
        }
    }
}