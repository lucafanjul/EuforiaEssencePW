using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EuforiaEssencePW.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        public GeminiService(
            HttpClient http,
            IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        public async Task<string> GenerarAsync(
            string instrucciones,
            string datos,
            CancellationToken cancellationToken,
            bool respuestaJson = false)
        {
            var clave = (
                _configuration["Gemini:ApiKey"] ??
                _configuration["GEMINI_API_KEY"])?.Trim();

            var modelo = _configuration["Gemini:Model"]?.Trim();

            if (string.IsNullOrWhiteSpace(clave))
            {
                throw new InvalidOperationException(
                    "Falta configurar Gemini:ApiKey en los secretos de usuario.");
            }

            if (string.IsNullOrWhiteSpace(modelo))
            {
                throw new InvalidOperationException(
                    "Falta configurar Gemini:Model en los secretos de usuario.");
            }

            var url =
                "https://generativelanguage.googleapis.com/v1beta/models/" +
                Uri.EscapeDataString(modelo) +
                ":generateContent";

            using var solicitud = new HttpRequestMessage(
                HttpMethod.Post, url);

            solicitud.Headers.Add("x-goog-api-key", clave);

            var configuracion = new Dictionary<string, object>
            {
                ["temperature"] = 0.2,
                ["maxOutputTokens"] = 4096
            };

            if (respuestaJson)
            {
                configuracion["responseMimeType"] = "application/json";

                configuracion["responseSchema"] = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        respuesta = new { type = "STRING" },
                        productos = new
                        {
                            type = "ARRAY",
                            items = new { type = "INTEGER" }
                        }
                    },
                    required = new[] { "respuesta", "productos" }
                };
            }

            solicitud.Content = JsonContent.Create(new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = instrucciones }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = datos }
                        }
                    }
                },
                generationConfig = configuracion
            });

            using var respuesta = await _http.SendAsync(
                solicitud, cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                var mensaje = respuesta.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests =>
                        "Gemini alcanzó el límite de solicitudes o la cuota disponible.",

                    HttpStatusCode.BadRequest =>
                        "Gemini rechazó la solicitud. Revisá la clave y el modelo configurados.",

                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "Gemini no autorizó el acceso. Revisá la clave y sus permisos.",

                    HttpStatusCode.NotFound =>
                        "El modelo configurado no está disponible. Revisá Gemini:Model.",

                    _ =>
                        "Gemini no pudo completar la solicitud. Intentá nuevamente."
                };

                throw new InvalidOperationException(
                    $"{mensaje} (HTTP {(int)respuesta.StatusCode}).");
            }

            await using var stream =
                await respuesta.Content.ReadAsStreamAsync(cancellationToken);

            using var documento = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            var raiz = documento.RootElement;

            if (!raiz.TryGetProperty("candidates", out var candidatos) ||
                candidatos.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Gemini no devolvió una respuesta para esta consulta.");
            }

            var candidato = candidatos[0];

            if (candidato.TryGetProperty("finishReason", out var motivo) &&
                motivo.GetString() == "MAX_TOKENS")
            {
                throw new InvalidOperationException(
                    "La respuesta alcanzó el límite de extensión. Probá una consulta más breve.");
            }

            if (!candidato.TryGetProperty("content", out var contenido) ||
                !contenido.TryGetProperty("parts", out var partes))
            {
                throw new InvalidOperationException(
                    "Gemini no devolvió contenido de texto.");
            }

            var textos = new List<string>();

            foreach (var parte in partes.EnumerateArray())
            {
                if (parte.TryGetProperty("thought", out var pensamiento) &&
                    pensamiento.ValueKind == JsonValueKind.True)
                {
                    continue;
                }

                if (parte.TryGetProperty("text", out var texto) &&
                    texto.ValueKind == JsonValueKind.String)
                {
                    textos.Add(texto.GetString() ?? string.Empty);
                }
            }

            var resultado = string.Join("\n", textos).Trim();

            if (string.IsNullOrWhiteSpace(resultado))
            {
                throw new InvalidOperationException(
                    "Gemini devolvió una respuesta vacía.");
            }

            return resultado;
        }
    }
}