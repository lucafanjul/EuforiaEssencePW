using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MarcasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarcasController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool EsAjax =>
            Request.Headers["X-Requested-With"].ToString()
                == "XMLHttpRequest";

        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            buscar = buscar?.Trim();
            var consulta = _context.Marcas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
                consulta = consulta.Where(m => m.Nombre.Contains(buscar));

            return View(await PaginacionAdmin.CargarAsync(
                consulta.OrderBy(m => m.Nombre).ThenBy(m => m.Id),
                pagina,
                ViewData));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            return View(marca);
        }

        public IActionResult Create()
        {
            var marca = new Marca();

            if (EsAjax)
                return PartialView("_FormularioMarca", marca);

            return View(marca);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre")] Marca marca)
        {
            await ValidarNombreAsync(marca, 0);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Marcas.Add(marca);
                    await _context.SaveChangesAsync();

                    TempData["Exito"] = "Marca creada correctamente.";

                    if (EsAjax)
                        return Json(new { success = true });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar. Verifique que el nombre no esté repetido.");
                }
            }

            if (EsAjax)
                return PartialView("_FormularioMarca", marca);

            return View(marca);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas.FindAsync(id);

            if (marca == null)
                return NotFound();

            if (EsAjax)
                return PartialView("_FormularioMarca", marca);

            return View(marca);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre")] Marca marca)
        {
            if (id != marca.Id)
                return NotFound();

            var existente = await _context.Marcas.FindAsync(id);

            if (existente == null)
                return NotFound();

            await ValidarNombreAsync(marca, id);

            if (ModelState.IsValid)
            {
                try
                {
                    existente.Nombre = marca.Nombre;

                    await _context.SaveChangesAsync();

                    TempData["Exito"] = "Marca modificada correctamente.";

                    if (EsAjax)
                        return Json(new { success = true });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Marcas.AnyAsync(m => m.Id == id))
                        return NotFound();

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo actualizar. Recargue la página e intente nuevamente.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar. Verifique que el nombre no esté repetido.");
                }
            }

            if (EsAjax)
                return PartialView("_FormularioMarca", marca);

            return View(marca);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas
                .Include(m => m.Perfumes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            if (!marca.PuedeEliminarse())
            {
                TempData["Error"] =
                    "No se puede eliminar la marca porque tiene perfumes asociados.";

                return RedirectToAction(nameof(Index));
            }

            return View(marca);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var marca = await _context.Marcas
                .Include(m => m.Perfumes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (marca == null)
                return NotFound();

            if (!marca.PuedeEliminarse())
            {
                TempData["Error"] =
                    "No se puede eliminar la marca porque tiene perfumes asociados.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Marcas.Remove(marca);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Marca eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No se pudo eliminar. Compruebe si tiene registros asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarNombreAsync(
            Marca marca,
            int idExcluir)
        {
            marca.Nombre = (marca.Nombre ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(marca.Nombre))
            {
                ModelState.AddModelError(
                    nameof(Marca.Nombre),
                    "El nombre es obligatorio.");
            }
            else if (await _context.Marcas.AnyAsync(m =>
                m.Nombre == marca.Nombre && m.Id != idExcluir))
            {
                ModelState.AddModelError(
                    nameof(Marca.Nombre),
                    "Ya existe una marca con ese nombre.");
            }
        }
    }
}