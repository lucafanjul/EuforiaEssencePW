using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Cliente,Administrador")]
    public class PagosController : Controller
    {
        private const int MaximoArchivo = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PagosController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string UsuarioId =>
            _userManager.GetUserId(User)!;
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> QrPago(int id)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == UsuarioId);

            if (pedido == null || !pedido.PuedePagarse())
                return NotFound();

            var enlace = Url.Action(
                nameof(Pagar),
                "Pagos",
                new { id = pedido.Id },
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(enlace))
                return Problem("No se pudo generar el enlace de pago de prueba.");

            using var generador = new QRCoder.QRCodeGenerator();
            using var datos = generador.CreateQrCode(
                enlace, QRCoder.QRCodeGenerator.ECCLevel.M);
            using var qr = new QRCoder.PngByteQRCode(datos);

            return File(qr.GetGraphic(8), "image/png");
        }
        [HttpGet]
        public async Task<IActionResult> Pagar(int id)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.UsuarioId == UsuarioId);

            if (pedido == null)
                return NotFound();

            var historial = await Resumen(
                _context.ComprobantesPago
                    .AsNoTracking()
                    .Where(c => c.PedidoId == id))
                .OrderByDescending(c => c.FechaEnvio)
                .ToListAsync();

            return View(new PagoComprobanteViewModel
            {
                Pedido = pedido,
                Token = Guid.NewGuid(),
                Historial = historial
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Subir(
    int id,
    Guid token,
    IFormFile? archivo,
    CancellationToken cancellationToken,
    string? motivoApelacion = null)
        {
            if (!HttpContext.RequestServices
    .GetRequiredService<IConfiguration>()
    .GetValue<bool>("Tienda:PermitirCargaDeComprobantes"))
            {
                return NotFound();
            }
            if (token == Guid.Empty)
                return BadRequest();

            if (archivo == null ||
                archivo.Length == 0 ||
                archivo.Length > MaximoArchivo)
            {
                TempData["Error"] =
                    "Seleccioná un archivo PDF, JPG o PNG de hasta 5 MB.";

                return RedirectToAction(nameof(Pagar), new { id });
            }

            var extension = Path.GetExtension(
                archivo.FileName).ToLowerInvariant();

            byte[] contenido;

            using (var memoria = new MemoryStream())
            {
                await archivo.CopyToAsync(
                    memoria, cancellationToken);

                if (memoria.Length == 0 ||
                    memoria.Length > MaximoArchivo)
                {
                    TempData["Error"] =
                        "El archivo debe tener contenido y no superar 5 MB.";

                    return RedirectToAction(nameof(Pagar), new { id });
                }

                contenido = memoria.ToArray();
            }

            var tipo = ObtenerTipo(contenido, extension);

            if (tipo == null)
            {
                TempData["Error"] =
                    "El contenido del archivo no coincide con un PDF, JPG o PNG válido.";

                return RedirectToAction(nameof(Pagar), new { id });
            }
            return await RegistrarComprobante(
                id, token, contenido, tipo, extension,
                motivoApelacion: motivoApelacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Simular(int id, Guid token, string resultado)
        {
            if (token == Guid.Empty ||
                (resultado != "Aprobado" && resultado != "Rechazado"))
            {
                return BadRequest();
            }

            return await RegistrarComprobante(
                id, token, Array.Empty<byte>(), "text/plain", ".txt", resultado);
        }

        private async Task<IActionResult> RegistrarComprobante(
            int id,
            Guid token,
            byte[] contenido,
            string tipo,
            string extension,
            string? resultadoSimulado = null,
            string? motivoApelacion = null)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Detalles)
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.UsuarioId == UsuarioId);

                if (pedido == null)
                    return NotFound();

                var existente = await _context.ComprobantesPago
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == token);

                if (existente != null)
                {
                    if (existente.PedidoId != pedido.Id)
                        return BadRequest();

                    TempData["Exito"] =
                        "Este envío ya fue recibido.";

                    return RedirectToAction(nameof(Pagar), new { id });
                }

                if (!pedido.PuedePagarse())
                {
                    TempData["Error"] =
                        "El pedido no admite otro comprobante en este momento.";

                    return RedirectToAction(nameof(Pagar), new { id });
                }

                if (!pedido.Detalles.Any() || pedido.Total <= 0)
                {
                    TempData["Error"] =
                        "El pedido no tiene un importe válido.";

                    return RedirectToAction(nameof(Pagar), new { id });
                }
                motivoApelacion = motivoApelacion?.Trim();

                var esApelacion = resultadoSimulado == null &&
                                  pedido.EstadoPago == "Rechazado" &&
                                  pedido.StockLiberado;

                if (esApelacion &&
                    (string.IsNullOrWhiteSpace(motivoApelacion) || motivoApelacion.Length > 500))
                {
                    TempData["Error"] =
                        "Para apelar, escribí una explicación de hasta 500 caracteres.";

                    return RedirectToAction(nameof(Pagar), new { id });
                }
                var rechazado = resultadoSimulado == "Rechazado";
                var ahora = DateTime.UtcNow;
                if (resultadoSimulado != null)
                {
                    var cultura = System.Globalization.CultureInfo.GetCultureInfo("es-AR");
                    var importe = pedido.Total.ToString("C2", cultura);
                    var fechaArgentina = ahora.AddHours(-3);

                    var texto =
                        $"Pedido: #{pedido.Id}\n" +
                        $"Importe simulado: {importe}\n" +
                        $"Resultado de la simulación: {resultadoSimulado}\n" +
                        $"Fecha: {fechaArgentina:dd/MM/yyyy HH:mm} (Argentina)\n" +
                        "No se realizó ningún cobro ni transferencia.";

                    contenido = GenerarComprobanteSimuladoPdf(texto);
                    tipo = "application/pdf";
                    extension = ".pdf";
                }

                _context.ComprobantesPago.Add(new ComprobantePago
                {
                    Id = token,
                    PedidoId = pedido.Id,
                    Contenido = contenido,
                    TipoContenido = tipo,
                    Extension = extension,
                    Estado = rechazado ? "Rechazado" : "Pendiente",
                    ImporteEsperado = pedido.Total,
                    MotivoApelacion = esApelacion ? motivoApelacion : null,
                    FechaEnvio = ahora,
                    MotivoRechazo = rechazado ? "Rechazo simulado por el cliente." : null
                });

                pedido.EstadoPago = rechazado ? "Rechazado" : "En revisión";
                pedido.PagoValidadoPorAdmin = false;
                pedido.FechaPago = null;
                pedido.ReferenciaPago = null;

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                if (rechazado)
                {
                    TempData["Error"] = "Pago de prueba rechazado. Podés reintentar.";
                }
                else
                {
                    TempData["Exito"] = "Comprobante registrado. Está pendiente de revisión.";
                }
            }
            catch (Exception ex) when (
                ex is DbUpdateException ||
                ex is Microsoft.Data.SqlClient.SqlException)
            {
                await transaccion.RollbackAsync();
                _context.ChangeTracker.Clear();
                await HttpContext.RequestServices
    .GetRequiredService<
        EuforiaEssencePW.Services.RegistroErroresService>()
    .RegistrarAsync("OPERACION_NO_COMPLETADA", ex);
                TempData["Error"] =
                    "No se pudo registrar el envío. Revisá el historial antes de volver a intentarlo.";
            }

            return RedirectToAction(nameof(Pagar), new { id });
        }
        private static byte[] GenerarComprobanteSimuladoPdf(string contenido)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(40);
                    pagina.DefaultTextStyle(estilo => estilo.FontSize(11));

                    pagina.Header().Column(columna =>
                    {
                        columna.Spacing(6);
                        columna.Item().Text("Euphoria Essence")
                            .FontSize(24).Bold().FontColor("#795B38");
                        columna.Item().Text("Comprobante de pago de prueba")
                            .FontSize(16).SemiBold();
                    });

                    pagina.Content().PaddingTop(24).Column(columna =>
                    {
                        columna.Spacing(10);

                        foreach (var linea in contenido.Split('\n'))
                        {
                            var texto = linea.Trim();

                            if (!string.IsNullOrWhiteSpace(texto) &&
                                texto != "Euphoria Essence — Comprobante de prueba")
                            {
                                columna.Item().Text(texto);
                            }
                        }
                    });

                    pagina.Footer().AlignCenter()
                        .Text("Tienda demo · Sin validez fiscal")
                        .FontSize(9).FontColor("#666666");
                });
            }).GeneratePdf();
        }
        [HttpGet]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Descargar(Guid id)
        {
            var esAdministrador = User.IsInRole("Administrador");

            var comprobante = await _context.ComprobantesPago
                .AsNoTracking()
                .Where(c =>
                    c.Id == id &&
                    (esAdministrador ||
                     c.Pedido.UsuarioId == UsuarioId))
                .Select(c => new
                {
                    c.Contenido,
                    c.TipoContenido,
                    c.Extension,
                    c.PedidoId
                })
                .FirstOrDefaultAsync();

            if (comprobante == null)
                return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (comprobante.Extension == ".txt" &&
    comprobante.TipoContenido == "text/plain")
            {
                var texto = System.Text.Encoding.UTF8.GetString(comprobante.Contenido);

                return File(
                    GenerarComprobanteSimuladoPdf(texto),
                    "application/pdf",
                    $"Comprobante-pago-pedido-{comprobante.PedidoId}.pdf");
            }
            return File(
                comprobante.Contenido,
                comprobante.TipoContenido,
                $"Comprobante-pago-pedido-{comprobante.PedidoId}" +
                comprobante.Extension);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Revisiones(
     string estado = "Pendiente",
     int pagina = 1,
     int? pedidoId = null)
        {
            var estados = new[] { "Pendiente", "Aprobado", "Rechazado", "Todos" };

            if (!estados.Contains(estado))
                estado = "Pendiente";

            if (pedidoId.HasValue &&
                !await _context.Pedidos.AnyAsync(p => p.Id == pedidoId.Value))
            {
                return NotFound();
            }

            var consulta = _context.ComprobantesPago.AsNoTracking();

            if (pedidoId.HasValue)
                consulta = consulta.Where(c => c.PedidoId == pedidoId.Value);

            if (estado != "Todos")
                consulta = consulta.Where(c => c.Estado == estado);

            if (estado == "Pendiente")
            {
                consulta = consulta.Where(c =>
                    c.Pedido.Estado == "Pendiente" &&
                    c.Pedido.EstadoPago == "En revisión");
            }

            const int porPagina = 30;

            var total = await consulta.CountAsync();
            var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina));
            pagina = Math.Clamp(pagina, 1, paginas);

            ViewBag.Estado = estado;
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = paginas;
            ViewBag.PedidoId = pedidoId;

            var registros = await Resumen(consulta)
                .OrderByDescending(c => c.FechaEnvio)
                .ThenBy(c => c.Id)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            return View(registros);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Revisar(
     Guid id,
     string decision,
     string? motivo,
     int? pedidoId = null)
        {
            IActionResult Volver() => RedirectToAction(nameof(Revisiones), new
            {
                pedidoId,
                estado = pedidoId.HasValue ? "Todos" : "Pendiente"
            });

            motivo = motivo?.Trim();

            if (decision != "Aprobado" && decision != "Rechazado")
                return BadRequest();

            var aprobado = decision == "Aprobado";

            if ((motivo?.Length ?? 0) > 500 ||
                (!aprobado && string.IsNullOrWhiteSpace(motivo)))
            {
                TempData["Error"] = "Para rechazar, escribí un motivo de hasta 500 caracteres.";
                return Volver();
            }

            await using var transaccion = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var comprobante = await _context.ComprobantesPago
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comprobante == null ||
                    (pedidoId.HasValue && pedidoId.Value != comprobante.PedidoId))
                {
                    return NotFound();
                }

                var pedido = await _context.Pedidos
                    .Include(p => p.Detalles)
                    .FirstOrDefaultAsync(p => p.Id == comprobante.PedidoId);

                if (pedido == null)
                    return NotFound();

                if (comprobante.Estado != "Pendiente" ||
                    pedido.Estado != "Pendiente" ||
                    pedido.EstadoPago != "En revisión")
                {
                    TempData["Error"] =
                        "El comprobante ya fue revisado o el pedido cambió de estado.";

                    return Volver();
                }

                if (pedido.Total != comprobante.ImporteEsperado)
                {
                    TempData["Error"] = "El importe del pedido cambió.";
                    return Volver();
                }

                if (aprobado)
                {
                    var reservado = await EuforiaEssencePW.Services.StockPedidoService
                        .IntentarReservarAsync(_context, pedido);

                    if (!reservado)
                    {
                        TempData["Error"] =
                            "No hay stock suficiente o alguna presentación ya no está disponible. " +
                            "El comprobante sigue en revisión.";

                        return Volver();
                    }
                }
                else
                {
                    await EuforiaEssencePW.Services.StockPedidoService
                        .LiberarAsync(_context, pedido);
                }

                var ahora = DateTime.UtcNow;

                comprobante.Estado = decision;
                comprobante.FechaRevision = ahora;
                comprobante.RevisadoPorId = UsuarioId;
                comprobante.MotivoRechazo = aprobado ? null : motivo;

                pedido.EstadoPago = decision;
                pedido.PagoValidadoPorAdmin = aprobado;
                pedido.FechaPago = aprobado ? ahora : null;
                pedido.ReferenciaPago = aprobado ? $"COMP-{comprobante.Id:N}" : null;

                _context.IntentosPago.Add(new IntentoPago
                {
                    Id = Guid.NewGuid(),
                    PedidoId = pedido.Id,
                    Resultado = decision,
                    Importe = comprobante.ImporteEsperado,
                    Fecha = ahora
                });

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["Exito"] = aprobado
                    ? "Pago validado. El pedido puede pasar a preparación."
                    : "Comprobante rechazado. Las unidades quedaron disponibles en el catálogo.";
            }
            catch (Exception ex) when (
                ex is DbUpdateException ||
                ex is Microsoft.Data.SqlClient.SqlException ||
                ex is InvalidOperationException ||
                ex is OverflowException)
            {
                await transaccion.RollbackAsync();
                _context.ChangeTracker.Clear();

                await HttpContext.RequestServices
                    .GetRequiredService<EuforiaEssencePW.Services.RegistroErroresService>()
                    .RegistrarAsync("OPERACION_NO_COMPLETADA", ex);

                TempData["Error"] =
                    "No se pudo guardar la revisión. Recargá antes de volver a intentar.";
            }

            return Volver();
        }

        private IQueryable<ComprobanteResumenViewModel> Resumen(
            IQueryable<ComprobantePago> consulta)
        {
            return consulta.Select(c => new ComprobanteResumenViewModel
            {
                Id = c.Id,
                PedidoId = c.PedidoId,
                Cliente = c.Pedido.Usuario == null
                    ? ""
                    : c.Pedido.Usuario.Email ?? "",
                ImporteEsperado = c.ImporteEsperado,
                FechaEnvio = c.FechaEnvio,
                FechaRevision = c.FechaRevision,
                Estado = c.Estado,
                EstadoPedido = c.Pedido.Estado,
                MotivoRechazo = c.MotivoRechazo,
                MotivoApelacion = c.MotivoApelacion,
                PuedeRevisarse =
                    c.Estado == "Pendiente" &&
                    c.Pedido.Estado == "Pendiente" &&
                    c.Pedido.EstadoPago == "En revisión"
            });
        }

        private static string? ObtenerTipo(
            byte[] contenido,
            string extension)
        {
            if (extension == ".pdf" &&
                contenido.Take(5).SequenceEqual(
                    new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }))
            {
                return "application/pdf";
            }

            if (extension == ".png" &&
                contenido.Take(8).SequenceEqual(
                    new byte[]
                    {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                    }))
            {
                return "image/png";
            }

            if ((extension == ".jpg" || extension == ".jpeg") &&
                contenido.Take(3).SequenceEqual(
                    new byte[] { 0xFF, 0xD8, 0xFF }))
            {
                return "image/jpeg";
            }

            return null;
        }
    }
}