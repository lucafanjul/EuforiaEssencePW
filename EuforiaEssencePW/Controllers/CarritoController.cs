using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Cliente,Administrador")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public CarritoController(
            ApplicationDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string UsuarioId =>
            _userManager.GetUserId(User)!;

        private string ClaveCarrito =>
            "Carrito:" + UsuarioId;

        private Dictionary<int, int> LeerCarrito()
        {
            var contenido = HttpContext.Session.GetString(ClaveCarrito);

            if (string.IsNullOrWhiteSpace(contenido))
                return new Dictionary<int, int>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<int, int>>(
                    contenido) ?? new Dictionary<int, int>();
            }
            catch (JsonException)
            {
                return new Dictionary<int, int>();
            }
        }

        private void GuardarCarrito(Dictionary<int, int> carrito)
        {
            HttpContext.Session.SetString(
                ClaveCarrito,
                JsonSerializer.Serialize(carrito));

            // Si cambia el carrito, debe revisarse el resumen de compra.
            HttpContext.Session.Remove("Confirmacion:" + UsuarioId);
        }

        private async Task<CarritoViewModel> ArmarCarritoAsync()
        {
            var carrito = LeerCarrito();
            var ids = carrito.Keys.ToList();

            var presentaciones = await _context.Presentaciones
                .AsNoTracking()
                .Include(p => p.Perfume)
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var modelo = new CarritoViewModel();

            foreach (var item in carrito)
            {
                presentaciones.TryGetValue(
                    item.Key, out var presentacion);

                modelo.Lineas.Add(new CarritoLineaViewModel
                {
                    PresentacionId = item.Key,
                    Cantidad = item.Value,
                    Nombre = presentacion?.Perfume?.Nombre
                        ?? "Producto no disponible",
                    ImagenUrl = presentacion?.Perfume?.ImagenUrl,
                    Mililitros = presentacion?.Mililitros ?? 0,
                    Precio = presentacion?.Precio ?? 0,
                    Stock = presentacion?.Stock ?? 0,
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
        public async Task<IActionResult> Index()
        {
            return View(await ArmarCarritoAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Contenido()
        {
            return PartialView(
                "_Contenido",
                await ArmarCarritoAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(
            int presentacionId,
            int cantidad = 1)
        {
            if (!ModelState.IsValid || cantidad < 1 || cantidad > 999)
            {
                return Responder(
                    false,
                    "Ingresá una cantidad entre 1 y 999.",
                    400,
                    volverAlCatalogo: true);
            }

            var presentacion = await _context.Presentaciones
                .AsNoTracking()
                .Include(p => p.Perfume)
                .FirstOrDefaultAsync(p => p.Id == presentacionId);

            if (presentacion == null ||
                !presentacion.Activo ||
                presentacion.Perfume?.Activo != true)
            {
                return Responder(
                    false,
                    "La presentación no está disponible.",
                    400,
                    volverAlCatalogo: true);
            }

            var carrito = LeerCarrito();
            var actual = Math.Max(
                0, carrito.GetValueOrDefault(presentacionId));

            if (cantidad > 999 - actual ||
                (long)actual + cantidad > presentacion.Stock)
            {
                return Responder(
                    false,
                    "La cantidad supera el stock disponible o el límite de 999 unidades.",
                    400,
                    presentacion.PerfumeId);
            }

            if (!carrito.ContainsKey(presentacionId) &&
                carrito.Count >= 50)
            {
                return Responder(
                    false,
                    "El carrito admite hasta 50 presentaciones diferentes.",
                    400,
                    presentacion.PerfumeId);
            }

            carrito[presentacionId] = actual + cantidad;
            GuardarCarrito(carrito);

            return Responder(
                true,
                "Producto agregado al carrito.",
                perfumeId: presentacion.PerfumeId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(
            int presentacionId,
            int cantidad)
        {
            if (!ModelState.IsValid || cantidad < 1 || cantidad > 999)
            {
                return Responder(
                    false,
                    "Ingresá una cantidad entre 1 y 999.",
                    400);
            }

            var carrito = LeerCarrito();

            if (!carrito.TryGetValue(presentacionId, out var actual))
            {
                return Responder(
                    false,
                    "El producto ya no está en el carrito.",
                    400);
            }

            // Permite reducir incluso cuando el stock haya bajado.
            // La compra sigue bloqueada si la cantidad aún supera el stock.
            if (cantidad > actual)
            {
                var presentacion = await _context.Presentaciones
                    .AsNoTracking()
                    .Include(p => p.Perfume)
                    .FirstOrDefaultAsync(p => p.Id == presentacionId);

                if (presentacion == null ||
                    !presentacion.Activo ||
                    presentacion.Perfume?.Activo != true ||
                    cantidad > presentacion.Stock)
                {
                    return Responder(
                        false,
                        "No hay stock disponible para aumentar esa cantidad.",
                        400);
                }
            }

            carrito[presentacionId] = cantidad;
            GuardarCarrito(carrito);

            return Responder(true, "Cantidad actualizada.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarCantidad(
            int presentacionId,
            int variacion)
        {
            if (!ModelState.IsValid ||
                (variacion != -1 && variacion != 1))
            {
                return Responder(
                    false,
                    "La modificación de cantidad no es válida.",
                    400);
            }

            var carrito = LeerCarrito();

            if (!carrito.TryGetValue(presentacionId, out var actual))
            {
                return Responder(
                    false,
                    "El producto ya no está en el carrito.",
                    400);
            }

            if (actual < 1 || actual > 999)
            {
                return Responder(
                    false,
                    "Revisá la cantidad o quitá el producto.",
                    400);
            }

            return await Actualizar(
                presentacionId,
                actual + variacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Quitar(int presentacionId)
        {
            if (!ModelState.IsValid)
                return Responder(false, "Producto no válido.", 400);

            var carrito = LeerCarrito();

            // Quita toda la línea, sin importar su cantidad.
            carrito.Remove(presentacionId);
            GuardarCarrito(carrito);

            return Responder(true, "Producto eliminado del carrito.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove(ClaveCarrito);
            HttpContext.Session.Remove("Confirmacion:" + UsuarioId);

            return Responder(true, "Carrito vaciado.");
        }

        private IActionResult Responder(
            bool exito,
            string mensaje,
            int estado = 200,
            int? perfumeId = null,
            bool volverAlCatalogo = false)
        {
            var unidades = LeerCarrito()
                .Values
                .Where(c => c > 0)
                .Sum(c => (long)c);

            var esAjax = string.Equals(
                Request.Headers["X-Requested-With"].ToString(),
                "XMLHttpRequest",
                StringComparison.Ordinal);

            if (esAjax)
            {
                return StatusCode(estado, new
                {
                    mensaje,
                    unidades
                });
            }

            TempData[exito ? "Exito" : "Error"] = mensaje;

            if (perfumeId.HasValue)
            {
                return RedirectToAction(
                    "Details",
                    "Catalogo",
                    new { id = perfumeId.Value });
            }

            if (volverAlCatalogo)
                return RedirectToAction("Index", "Catalogo");

            return RedirectToAction(nameof(Index));
        }
    }
}