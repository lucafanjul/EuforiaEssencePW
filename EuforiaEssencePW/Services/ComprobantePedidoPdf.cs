using EuforiaEssencePW.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace EuforiaEssencePW.Services
{
    public static class ComprobantePedidoPdf
    {
        public static byte[] Generar(Pedido pedido)
        {
            var cultura = CultureInfo.GetCultureInfo("es-AR");

            var fechaPedido = EnArgentina(pedido.FechaPedido);
            var fechaEmision = EnArgentina(DateTime.UtcNow);

            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(36);
                    pagina.PageColor(Colors.White);

                    pagina.DefaultTextStyle(estilo =>
                        estilo.FontSize(10).FontColor("#262620"));

                    pagina.Header()
                        .PaddingBottom(18)
                        .Column(columna =>
                        {
                            columna.Item()
                                .Text("EUPHORIA ESSENCE")
                                .FontSize(22)
                                .SemiBold()
                                .FontColor("#76552B");

                            columna.Item()
                                .PaddingTop(5)
                                .Text("Comprobante de compra")
                                .FontSize(16)
                                .SemiBold();

                            columna.Item()
                                .PaddingTop(4)
                                .Text($"Pedido #{pedido.Id:D6}")
                                .FontSize(12);
                        });

                    pagina.Content().Column(columna =>
                    {
                        columna.Spacing(14);

                        columna.Item()
                            .Background("#F2EFE9")
                            .Padding(12)
                            .Column(datos =>
                            {
                                datos.Spacing(5);

                                datos.Item().Text(
                                    $"Fecha del pedido: " +
                                    $"{fechaPedido:dd/MM/yyyy HH:mm} — Argentina");

                                datos.Item().Text(
                                    $"Estado del pedido: {pedido.Estado}");

                                datos.Item().Text(
                                    $"Estado del pago: {pedido.EstadoPago}");

                                datos.Item().Text(
                                    $"Emitido: " +
                                    $"{fechaEmision:dd/MM/yyyy HH:mm} — Argentina");

                                if (pedido.FechaPago.HasValue)
                                {
                                    var fechaPago =
                                        EnArgentina(pedido.FechaPago.Value);

                                    datos.Item().Text(
                                        $"Fecha de aprobación del pago: " +
                                        $"{fechaPago:dd/MM/yyyy HH:mm}");
                                }

                                if (!string.IsNullOrWhiteSpace(
                                    pedido.ReferenciaPago))
                                {
                                    datos.Item().Text(
                                        $"Operación: {pedido.ReferenciaPago}");
                                }
                            });

                        columna.Item().Column(entrega =>
                        {
                            entrega.Spacing(5);

                            entrega.Item()
                                .Text("Dirección de envío")
                                .SemiBold()
                                .FontSize(12);

                            // Usamos la dirección guardada al comprar,
                            // aunque el cliente la modifique después.
                            entrega.Item().Text(
                                pedido.DireccionEntrega.Replace(" | ", "\n"));
                        });

                        columna.Item().Text("Productos")
                            .SemiBold()
                            .FontSize(12);

                        columna.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.RelativeColumn();
                                columnas.ConstantColumn(45);
                                columnas.ConstantColumn(40);
                                columnas.ConstantColumn(90);
                                columnas.ConstantColumn(90);
                            });

                            tabla.Header(encabezado =>
                            {
                                encabezado.Cell()
                                    .Element(CeldaEncabezado)
                                    .Text("Producto").SemiBold();

                                encabezado.Cell()
                                    .Element(CeldaEncabezado)
                                    .Text("Ml").SemiBold();

                                encabezado.Cell()
                                    .Element(CeldaEncabezado)
                                    .Text("Cant.").SemiBold();

                                encabezado.Cell()
                                    .Element(CeldaEncabezado)
                                    .AlignRight()
                                    .Text("Precio unit.").SemiBold();

                                encabezado.Cell()
                                    .Element(CeldaEncabezado)
                                    .AlignRight()
                                    .Text("Subtotal").SemiBold();
                            });

                            foreach (var detalle in pedido.Detalles
                                .OrderBy(d => d.NombrePerfume)
                                .ThenBy(d => d.Mililitros))
                            {
                                tabla.Cell()
                                    .Element(CeldaDato)
                                    .Text(detalle.NombrePerfume);

                                tabla.Cell()
                                    .Element(CeldaDato)
                                    .Text(detalle.Mililitros.ToString());

                                tabla.Cell()
                                    .Element(CeldaDato)
                                    .Text(detalle.Cantidad.ToString());

                                tabla.Cell()
                                    .Element(CeldaDato)
                                    .AlignRight()
                                    .Text(detalle.PrecioUnitario
                                        .ToString("C2", cultura));

                                tabla.Cell()
                                    .Element(CeldaDato)
                                    .AlignRight()
                                    .Text(detalle.Subtotal
                                        .ToString("C2", cultura));
                            }
                        });

                        columna.Item()
                            .AlignRight()
                            .Text(
                                $"Total de productos: " +
                                pedido.Total.ToString("C2", cultura))
                            .SemiBold()
                            .FontSize(15);

                        columna.Item().Text(
                            "Importes expresados en pesos argentinos.")
                            .FontSize(9);

                        if (!string.IsNullOrWhiteSpace(
                            pedido.Observaciones))
                        {
                            columna.Item().Column(observaciones =>
                            {
                                observaciones.Spacing(5);

                                observaciones.Item()
                                    .Text("Indicaciones de entrega")
                                    .SemiBold();

                                observaciones.Item()
                                    .Text(pedido.Observaciones);
                            });
                        }

                        if (pedido.Estado == "Cancelado")
                        {
                            columna.Item()
                                .Text("PEDIDO CANCELADO")
                                .SemiBold()
                                .FontColor("#A33333");
                        }

                        columna.Item()
                            .BorderTop(1)
                            .BorderColor("#DED9CE")
                            .PaddingTop(10)
                            .Text(
                                "Comprobante del sistema. No es una factura fiscal. " +
                                "La descarga no acredita ni aprueba un pago. " +
                                "El estado informado corresponde al momento de emisión.")
                            .FontSize(9)
                            .FontColor("#66665B");

                        columna.Item().Text(
                            "Proyecto académico: los pagos de prueba " +
                            "no representan cobros reales.")
                            .FontSize(9)
                            .FontColor("#66665B");
                    });

                    pagina.Footer()
                        .PaddingTop(12)
                        .AlignCenter()
                        .Text(texto =>
                        {
                            texto.Span("Página ");
                            texto.CurrentPageNumber();
                            texto.Span(" de ");
                            texto.TotalPages();
                        });
                });
            }).GeneratePdf();
        }

        private static DateTime EnArgentina(DateTime fecha)
        {
            return DateTime.SpecifyKind(
                fecha, DateTimeKind.Utc).AddHours(-3);
        }

        private static IContainer CeldaEncabezado(
            IContainer contenedor)
        {
            return contenedor
                .Background("#E8E2D8")
                .BorderBottom(1)
                .BorderColor("#C8BEAD")
                .Padding(6)
                .DefaultTextStyle(estilo => estilo.FontSize(9));
        }

        private static IContainer CeldaDato(
            IContainer contenedor)
        {
            return contenedor
                .BorderBottom(1)
                .BorderColor("#E5E0D7")
                .Padding(6)
                .DefaultTextStyle(estilo => estilo.FontSize(9));
        }
    }
}