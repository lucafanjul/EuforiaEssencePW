using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Cliente,Administrador")]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PedidosController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string UsuarioId =>
            _userManager.GetUserId(User)!;

        private string ClaveCarrito => "Carrito:" + UsuarioId;

        private string ClaveConfirmacion => "Confirmacion:" + UsuarioId;

        private Dictionary<int, int> LeerCarrito()
        {
            var json = HttpContext.Session.GetString(ClaveCarrito);

            return json == null
                ? new Dictionary<int, int>()
                : JsonSerializer.Deserialize<Dictionary<int, int>>(json)
                    ?? new Dictionary<int, int>();
        }

        // Firma del resumen mostrado al cliente.
        // Si cambian cantidades o precios, pedimos revisar nuevamente.
        private static string Firma(CarritoViewModel carrito)
        {
            var datos = carrito.Lineas
                .OrderBy(l => l.PresentacionId)
                .Select(l => new
                {
                    l.PresentacionId,
                    l.Cantidad,
                    l.Precio
                });

            var bytes = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(datos));

            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        private async Task<CarritoViewModel> ArmarCarritoAsync(
            Dictionary<int, int> carrito)
        {
            var ids = carrito.Keys.ToList();

            var presentaciones = await _context.Presentaciones
                .AsNoTracking()
                .Include(p => p.Perfume)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            var modelo = new CarritoViewModel();

            foreach (var item in carrito)
            {
                var presentacion = presentaciones
                    .FirstOrDefault(p => p.Id == item.Key);

                modelo.Lineas.Add(new CarritoLineaViewModel
                {
                    PresentacionId = item.Key,
                    Cantidad = item.Value,
                    Nombre = presentacion?.Perfume?.Nombre
                        ?? "Producto no disponible",
                    Mililitros = presentacion?.Mililitros ?? 0,
                    Precio = presentacion?.Precio ?? 0,
                    Stock = presentacion?.Stock ?? 0,
                    ImagenUrl = presentacion?.Perfume?.ImagenUrl,
                    Disponible = presentacion != null
                        && presentacion.Activo
                        && presentacion.Perfume?.Activo == true
                        && item.Value >= 1
                        && item.Value <= 999
                        && item.Value <= presentacion.Stock
                });
            }

            return modelo;
        }

        [HttpGet]
        public async Task<IActionResult> Confirmar()
        {
            var carrito = await ArmarCarritoAsync(LeerCarrito());

            if (!carrito.PuedeComprar)
            {
                TempData["Error"] =
                    "Revise el carrito antes de continuar.";

                return RedirectToAction("Index", "Carrito");
            }

            var token = Guid.NewGuid();

            HttpContext.Session.SetString(
                ClaveConfirmacion,
                token.ToString("N") + ":" + Firma(carrito));

            var direcciones = await _context.Direcciones
                .AsNoTracking()
                .Where(d => d.UsuarioId == UsuarioId)
                .OrderBy(d => d.Id)
                .ToListAsync();

            return View(new ConfirmarCompraViewModel
            {
                Token = token,
                Carrito = carrito,
                Direcciones = direcciones
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(
            ConfirmarCompraViewModel modelo)
        {
            // Si ya se confirmó, mostramos el mismo pedido.
            var existente = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.TokenConfirmacion == modelo.Token &&
                    p.UsuarioId == UsuarioId);

            if (existente != null)
            {
                return RedirectToAction(
                    "Pagar", "Pagos", new { id = existente.Id });
            }

            var carritoSesion = LeerCarrito();

            var confirmacion = HttpContext.Session
                .GetString(ClaveConfirmacion);

            if (modelo.Token == Guid.Empty ||
                confirmacion == null ||
                !confirmacion.StartsWith(modelo.Token.ToString("N") + ":"))
            {
                TempData["Error"] =
                    "La confirmación venció. Revise nuevamente la compra.";

                return RedirectToAction(nameof(Confirmar));
            }

            if (!ModelState.IsValid)
            {
                modelo.Carrito = await ArmarCarritoAsync(carritoSesion);

                modelo.Direcciones = await _context.Direcciones
                    .AsNoTracking()
                    .Where(d => d.UsuarioId == UsuarioId)
                    .OrderBy(d => d.Id)
                    .ToListAsync();

                return View(modelo);
            }

            var pedidoId = 0;

            // La validación de stock y su descuento ocurren
            // dentro de la misma transacción.
            await using (var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable))
            {
                try
                {
                    // Segunda comprobación dentro de la transacción.
                    existente = await _context.Pedidos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.TokenConfirmacion == modelo.Token &&
                            p.UsuarioId == UsuarioId);

                    if (existente != null)
                    {
                        await transaccion.RollbackAsync();

                        return RedirectToAction(
                            "Pagar", "Pagos", new { id = existente.Id });
                    }

                    var direccion = await _context.Direcciones
                        .FirstOrDefaultAsync(d =>
                            d.Id == modelo.DireccionId &&
                            d.UsuarioId == UsuarioId);

                    if (direccion == null)
                    {
                        await transaccion.RollbackAsync();

                        TempData["Error"] =
                            "Seleccione una dirección propia y válida.";

                        return RedirectToAction(nameof(Confirmar));
                    }

                    var carritoActual =
                        await ArmarCarritoAsync(carritoSesion);

                    if (!carritoActual.PuedeComprar ||
                        confirmacion !=
                            modelo.Token.ToString("N") + ":" + Firma(carritoActual))
                    {
                        await transaccion.RollbackAsync();

                        TempData["Error"] =
                            "Cambió el precio, el carrito o la disponibilidad. Revise el resumen antes de confirmar.";

                        return RedirectToAction(nameof(Confirmar));
                    }

                    var ids = carritoSesion.Keys.ToList();

                    var presentaciones = await _context.Presentaciones
                        .Include(p => p.Perfume)
                        .Where(p => ids.Contains(p.Id))
                        .OrderBy(p => p.Id)
                        .ToListAsync();

                    var pedido = new Pedido
                    {
                        UsuarioId = UsuarioId,
                        DireccionId = direccion.Id,
                        TokenConfirmacion = modelo.Token,
                        FechaPedido = DateTime.UtcNow,
                        Estado = "Pendiente",
                        Observaciones = modelo.Observaciones?.Trim(),
                        DireccionEntrega =
                            $"{direccion.Destinatario} | " +
                            $"{direccion.Calle} {direccion.Altura} " +
                            $"{direccion.PisoDepartamento} | " +
                            $"{direccion.Localidad}, {direccion.Provincia} | " +
                            $"CP {direccion.CodigoPostal}"
                    };

                    foreach (var presentacion in presentaciones)
                    {
                        var cantidad = carritoSesion[presentacion.Id];

                        if (!presentacion.Activo ||
                            presentacion.Perfume?.Activo != true ||
                            cantidad < 1 ||
                            cantidad > presentacion.Stock)
                        {
                            throw new InvalidOperationException(
                                "Cambió la disponibilidad de un producto.");
                        }

                        pedido.Detalles.Add(new DetallePedido
                        {
                            PresentacionId = presentacion.Id,
                            Cantidad = cantidad,
                            PrecioUnitario = presentacion.Precio,
                            NombrePerfume = presentacion.Perfume.Nombre,
                            Mililitros = presentacion.Mililitros
                        });

                        presentacion.Stock -= cantidad;
                    }

                    _context.Pedidos.Add(pedido);

                    // También registra la auditoría dentro
                    // de esta misma transacción.
                    await _context.SaveChangesAsync();

                    await transaccion.CommitAsync();

                    pedidoId = pedido.Id;
                }
                catch (Exception ex) when (
                    ex is DbUpdateException ||
                    ex is Microsoft.Data.SqlClient.SqlException ||
                    ex is InvalidOperationException)
                {
                    await transaccion.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    await HttpContext.RequestServices
    .GetRequiredService<
        EuforiaEssencePW.Services.RegistroErroresService>()
    .RegistrarAsync("OPERACION_NO_COMPLETADA", ex);
                    // Otro envío simultáneo pudo confirmar el pedido.
                    var confirmado = await _context.Pedidos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.TokenConfirmacion == modelo.Token &&
                            p.UsuarioId == UsuarioId);

                    if (confirmado != null)
                    {
                        return RedirectToAction(
                            "Pagar", "Pagos", new { id = confirmado.Id });
                    }

                    TempData["Error"] =
                        "No se pudo confirmar la compra. No se registró el pedido. Revise la disponibilidad y vuelva a intentarlo.";

                    return RedirectToAction("Index", "Carrito");
                }
            }

            // Vaciamos la sesión solamente después del commit.
            HttpContext.Session.Remove(ClaveCarrito);
            HttpContext.Session.Remove(ClaveConfirmacion);

            TempData["Exito"] =
                "Pedido registrado. Continuá con el pago de prueba.";

            return RedirectToAction(
                "Pagar", "Pagos", new { id = pedidoId });
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .Where(p => p.UsuarioId == UsuarioId)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.UsuarioId == UsuarioId);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }
        [HttpGet]
        [ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> DescargarComprobante(
    int id,
    CancellationToken cancellationToken)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(
                    p => p.Id == id &&
                         p.UsuarioId == UsuarioId,
                    cancellationToken);

            if (pedido == null)
                return NotFound();

            var contenido =
                EuforiaEssencePW.Services.ComprobantePedidoPdf.Generar(pedido);

            return File(
                contenido,
                "application/pdf",
                $"EuphoriaEssence-Pedido-{pedido.Id:D6}.pdf");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var esAdministrador = User.IsInRole("Administrador");
            var controladorDestino = esAdministrador ? "GestionPedidos" : "Pedidos";

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Detalles)
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        (esAdministrador || p.UsuarioId == UsuarioId));

                if (pedido == null)
                    return NotFound();

                if (!pedido.PuedeCancelarse())
                {
                    TempData["Error"] = "Solo se pueden cancelar pedidos pendientes.";

                    return RedirectToAction(
                        "Details", controladorDestino, new { id });
                }

                var teniaStockReservado = !pedido.StockLiberado;

                await EuforiaEssencePW.Services.StockPedidoService
                    .LiberarAsync(_context, pedido);

                pedido.Estado = "Cancelado";

                if (pedido.EstadoPago == "Aprobado")
                {
                    pedido.EstadoPago = "Reintegrado";
                }
                else if (pedido.EstadoPago != "No requerido")
                {
                    pedido.EstadoPago = "Cancelado";
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["Exito"] = teniaStockReservado
                    ? "Pedido cancelado. Las unidades se devolvieron al stock."
                    : "Pedido cancelado. Las unidades ya se habían devuelto al stock.";
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
                    "No se pudo cancelar el pedido. No se aplicaron cambios.";
            }

            return RedirectToAction("Details", controladorDestino, new { id });
        }
    }
}