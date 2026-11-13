using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class PresentacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PresentacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? buscar, int? mililitros, string? stock, bool? activo, int pagina = 1)
        {
            buscar = buscar?.Trim();

            var consulta = _context.Presentaciones
                .Include(p => p.Perfume)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
                consulta = consulta.Where(p => p.Perfume != null && p.Perfume.Nombre.Contains(buscar));

            if (mililitros.HasValue)
                consulta = consulta.Where(p => p.Mililitros == mililitros.Value);

            if (activo.HasValue)
                consulta = consulta.Where(p => p.Activo == activo.Value);

            if (stock == "con")
                consulta = consulta.Where(p => p.Stock > 0);
            else if (stock == "sin")
                consulta = consulta.Where(p => p.Stock <= 0);

            return View(await PaginacionAdmin.CargarAsync(
                consulta.OrderBy(p => p.Perfume!.Nombre)
                    .ThenBy(p => p.Mililitros)
                    .ThenBy(p => p.Id),
                pagina,
                ViewData));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var presentacion = await _context.Presentaciones
                .Include(p => p.Perfume)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presentacion == null)
                return NotFound();

            return View(presentacion);
        }

        public async Task<IActionResult> Create()
        {
            await CargarPerfumesAsync();
            return View(new Presentacion());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PerfumeId,Mililitros,Precio,Stock,Activo")]
            Presentacion presentacion)
        {
            await ValidarAsync(presentacion, 0);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Presentaciones.Add(presentacion);
                    await _context.SaveChangesAsync();

                    TempData["Exito"] =
                        "Presentación creada correctamente.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar. Verifique que el perfume exista y que el tamaño no esté repetido.");
                }
            }

            await CargarPerfumesAsync(presentacion.PerfumeId);
            return View(presentacion);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var presentacion = await _context.Presentaciones.FindAsync(id);

            if (presentacion == null)
                return NotFound();

            await CargarPerfumesAsync(presentacion.PerfumeId);
            return View(presentacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,PerfumeId,Mililitros,Precio,Stock,Activo")]
            Presentacion presentacion)
        {
            if (id != presentacion.Id)
                return NotFound();

            var existente = await _context.Presentaciones.FindAsync(id);

            if (existente == null)
                return NotFound();

            await ValidarAsync(presentacion, id);

            if (ModelState.IsValid)
            {
                try
                {
                    existente.PerfumeId = presentacion.PerfumeId;
                    existente.Mililitros = presentacion.Mililitros;
                    existente.Precio = presentacion.Precio;
                    existente.Stock = presentacion.Stock;
                    existente.Activo = presentacion.Activo;

                    await _context.SaveChangesAsync();

                    TempData["Exito"] =
                        "Presentación modificada correctamente.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Presentaciones.AnyAsync(p => p.Id == id))
                        return NotFound();

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo actualizar. Vuelva a intentarlo.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar. Verifique que el tamaño no esté repetido.");
                }
            }

            await CargarPerfumesAsync(presentacion.PerfumeId);
            return View(presentacion);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var presentacion = await _context.Presentaciones
                .Include(p => p.Perfume)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presentacion == null)
                return NotFound();

            return View(presentacion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var presentacion = await _context.Presentaciones.FindAsync(id);

            if (presentacion == null)
                return NotFound();

            try
            {
                _context.Presentaciones.Remove(presentacion);
                await _context.SaveChangesAsync();

                TempData["Exito"] =
                    "Presentación eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No se pudo eliminar porque tiene registros asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarPerfumesAsync(int? seleccionado = null)
        {
            ViewData["PerfumeId"] = new SelectList(
                await _context.Perfumes
                    .OrderBy(p => p.Nombre)
                    .ToListAsync(),
                "Id",
                "Nombre",
                seleccionado);
        }

        private async Task ValidarAsync(
            Presentacion presentacion,
            int idExcluir)
        {
            if (!await _context.Perfumes
                .AnyAsync(p => p.Id == presentacion.PerfumeId))
            {
                ModelState.AddModelError(
                    nameof(Presentacion.PerfumeId),
                    "Seleccione un perfume existente.");
            }

            if (await _context.Presentaciones.AnyAsync(p =>
                p.Id != idExcluir &&
                p.PerfumeId == presentacion.PerfumeId &&
                p.Mililitros == presentacion.Mililitros))
            {
                ModelState.AddModelError(
                    nameof(Presentacion.Mililitros),
                    "Este perfume ya tiene una presentación de ese tamaño.");
            }
        }
    }
}