using EuforiaEssencePW.Services;

namespace EuforiaEssencePW.Middleware
{
    public class RegistroErroresMiddleware
    {
        private readonly RequestDelegate _next;

        public RegistroErroresMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            RegistroErroresService errores)
        {
            try
            {
                await _next(context);

                var estado = context.Response.StatusCode;

                if (estado >= 500 ||
                    estado is 400 or 403 or 409 or 413 or 429)
                {
                    await errores.RegistrarAsync(
                        $"HTTP_{estado}",
                        estadoHttp: estado);
                }
            }
            catch (Exception ex)
                when (!context.RequestAborted.IsCancellationRequested)
            {
                await errores.RegistrarAsync(
                    "EXCEPCION_NO_CONTROLADA",
                    ex,
                    estadoHttp: 500);

                throw;
            }
        }
    }
}