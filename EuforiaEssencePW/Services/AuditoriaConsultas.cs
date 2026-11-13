using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using System.Text.Json;

namespace EuforiaEssencePW.Services
{
    public static class AuditoriaConsultas
    {
        public static IReadOnlyDictionary<string, string> Secciones
        { get; } = new Dictionary<string, string>
        {
            ["Todos"] = "Todas",
            ["Ventas"] = "Ventas y pedidos",
            ["Pagos"] = "Pagos",
            ["Perfumes"] = "Perfumes",
            ["Marcas"] = "Marcas",
            ["Categorias"] = "Categorías",
            ["Presentaciones"] = "Presentaciones",
            ["Campanias"] = "Campañas",
            ["Errores"] = "Errores"
        };

        public static IQueryable<RegistroAuditoria> Aplicar(
            IQueryable<RegistroAuditoria> consulta,
            FiltroAuditoriaViewModel filtro)
        {
            var seccion = string.IsNullOrWhiteSpace(filtro.Seccion)
                ? "Todos"
                : filtro.Seccion;

            consulta = seccion switch
            {
                "Todos" => consulta,

                "Ventas" => consulta.Where(a =>
                    a.Entidad == "Pedido" ||
                    a.Entidad == "DetallePedido"),

                "Pagos" => consulta.Where(a =>
                    a.Entidad == "IntentoPago" ||
                    a.Entidad == "ComprobantePago"),

                "Perfumes" => consulta.Where(a =>
                    a.Entidad == "Perfume"),

                "Marcas" => consulta.Where(a =>
                    a.Entidad == "Marca"),

                "Categorias" => consulta.Where(a =>
                    a.Entidad == "Categoria"),

                "Presentaciones" => consulta.Where(a =>
                    a.Entidad == "Presentacion"),

                "Campanias" => consulta.Where(a =>
                    a.Entidad == "Campania" ||
                    a.Entidad == "CampaniaPerfume"),

                "Errores" => consulta.Where(a => a.Accion == "Error"),

                _ => consulta.Where(a => false)
            };

            if (filtro.Desde.HasValue)
            {
                var inicio = ConvertirUtc(filtro.Desde.Value.Date);
                consulta = consulta.Where(a => a.Fecha >= inicio);
            }

            if (filtro.Hasta.HasValue)
            {
                var fin = ConvertirUtc(
                    filtro.Hasta.Value.Date.AddDays(1));

                consulta = consulta.Where(a => a.Fecha < fin);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Entidad))
            {
                var entidad = filtro.Entidad.Trim();
                consulta = consulta.Where(a => a.Entidad == entidad);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Accion))
            {
                var accion = filtro.Accion.Trim();
                consulta = consulta.Where(a => a.Accion == accion);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                var usuario = filtro.Usuario.Trim();

                consulta = consulta.Where(a =>
                    a.UsuarioNombre.Contains(usuario));
            }

            return consulta;
        }

        public static string PaginaError(string json)
        {
            try
            {
                using var documento = JsonDocument.Parse(json);

                if (documento.RootElement.ValueKind ==
                        JsonValueKind.Object &&
                    documento.RootElement.TryGetProperty(
                        "Pagina", out var pagina))
                {
                    return pagina.ToString();
                }
            }
            catch (JsonException)
            {
                // Un registro antiguo puede no tener estos datos.
            }

            return "Sistema";
        }

        private static DateTime ConvertirUtc(DateTime fecha)
        {
            return new DateTimeOffset(
                DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified),
                TimeSpan.FromHours(-3)).UtcDateTime;
        }
    }
}