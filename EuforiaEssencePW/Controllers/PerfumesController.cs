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
    public class PerfumesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PerfumesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(
            string? buscar, int? marcaId, int? categoriaId, bool? activo, int pagina = 1)
        {
            buscar = buscar?.Trim();

            var consulta = _context.Perfumes
                .Include(p => p.Marca)
                .Include(p => p.Categoria)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
                consulta = consulta.Where(p => p.Nombre.Contains(buscar));

            if (marcaId.HasValue)
                consulta = consulta.Where(p => p.MarcaId == marcaId.Value);

            if (categoriaId.HasValue)
                consulta = consulta.Where(p => p.CategoriaId == categoriaId.Value);

            if (activo.HasValue)
                consulta = consulta.Where(p => p.Activo == activo.Value);

            ViewData["MarcasFiltro"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Marcas.AsNoTracking().OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre", marcaId);

            ViewData["CategoriasFiltro"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", categoriaId);

            return View(await PaginacionAdmin.CargarAsync(
                consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Id),
                pagina,
                ViewData));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var perfume = await _context.Perfumes
                .Include(p => p.Marca)
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perfume == null)
                return NotFound();

            return View(perfume);
        }

        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new Perfume());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Descripcion,NotasOlfativas,Activo,MarcaId,CategoriaId")]
            Perfume perfume,
            IFormFile? imagen)
        {
            await ValidarRelacionesAsync(perfume);
            await ValidarImagenAsync(imagen);

            string? nuevaRuta = null;

            if (ModelState.IsValid)
            {
                try
                {
                    nuevaRuta = await GuardarImagenAsync(imagen);
                    perfume.ImagenUrl = nuevaRuta;

                    _context.Perfumes.Add(perfume);
                    await _context.SaveChangesAsync();

                    TempData["Exito"] = "Perfume creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    EliminarImagenNueva(nuevaRuta);

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar el perfume. Revise los datos.");
                }
                catch (IOException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar la imagen. Intente nuevamente.");
                }
                catch (UnauthorizedAccessException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar la imagen en el servidor.");
                }
            }

            await CargarListasAsync(perfume);
            return View(perfume);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var perfume = await _context.Perfumes.FindAsync(id);

            if (perfume == null)
                return NotFound();

            await CargarListasAsync(perfume);
            return View(perfume);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre,Descripcion,NotasOlfativas,Activo,MarcaId,CategoriaId")]
            Perfume perfume,
            IFormFile? imagen)
        {
            if (id != perfume.Id)
                return NotFound();

            var existente = await _context.Perfumes.FindAsync(id);

            if (existente == null)
                return NotFound();

            var imagenAnterior = existente.ImagenUrl;
            perfume.ImagenUrl = imagenAnterior;

            await ValidarRelacionesAsync(perfume);
            await ValidarImagenAsync(imagen);

            string? nuevaRuta = null;

            if (ModelState.IsValid)
            {
                try
                {
                    nuevaRuta = await GuardarImagenAsync(imagen);

                    existente.Nombre = perfume.Nombre;
                    existente.Descripcion = perfume.Descripcion;
                    existente.NotasOlfativas = perfume.NotasOlfativas;
                    existente.Activo = perfume.Activo;
                    existente.MarcaId = perfume.MarcaId;
                    existente.CategoriaId = perfume.CategoriaId;

                    if (nuevaRuta != null)
                        existente.ImagenUrl = nuevaRuta;

                    await _context.SaveChangesAsync();

                    TempData["Exito"] = "Perfume modificado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    EliminarImagenNueva(nuevaRuta);

                    if (!await _context.Perfumes.AnyAsync(p => p.Id == id))
                        return NotFound();

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo actualizar el perfume. Vuelva a intentarlo.");
                }
                catch (DbUpdateException)
                {
                    EliminarImagenNueva(nuevaRuta);

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar el perfume. Revise los datos.");
                }
                catch (IOException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar la imagen. Intente nuevamente.");
                }
                catch (UnauthorizedAccessException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar la imagen en el servidor.");
                }
            }

            perfume.ImagenUrl = imagenAnterior;
            await CargarListasAsync(perfume);
            return View(perfume);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var perfume = await _context.Perfumes
                .Include(p => p.Marca)
                .Include(p => p.Categoria)
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perfume == null)
                return NotFound();

            if (!perfume.PuedeEliminarse())
            {
                TempData["Error"] =
                    "Tiene presentaciones asociadas. Puede desactivarlo desde Editar.";

                return RedirectToAction(nameof(Index));
            }

            return View(perfume);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var perfume = await _context.Perfumes
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perfume == null)
                return NotFound();

            if (!perfume.PuedeEliminarse())
            {
                TempData["Error"] =
                    "No se puede eliminar porque tiene presentaciones asociadas.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Perfumes.Remove(perfume);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Perfume eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No se pudo eliminar. Compruebe si tiene registros asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarListasAsync(Perfume? perfume = null)
        {
            ViewData["MarcaId"] = new SelectList(
                await _context.Marcas.OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre", perfume?.MarcaId);

            ViewData["CategoriaId"] = new SelectList(
                await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", perfume?.CategoriaId);
        }

        private async Task ValidarRelacionesAsync(Perfume perfume)
        {
            if (!await _context.Marcas.AnyAsync(m => m.Id == perfume.MarcaId))
            {
                ModelState.AddModelError(
                    nameof(Perfume.MarcaId),
                    "Seleccione una marca existente.");
            }

            if (!await _context.Categorias.AnyAsync(c => c.Id == perfume.CategoriaId))
            {
                ModelState.AddModelError(
                    nameof(Perfume.CategoriaId),
                    "Seleccione una categoría existente.");
            }
        }

        private async Task ValidarImagenAsync(IFormFile? imagen)
        {
            if (imagen == null)
                return;

            if (imagen.Length == 0 || imagen.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "imagen",
                    "Seleccione una imagen de hasta 5 MB.");
                return;
            }

            var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            var mime = imagen.ContentType.ToLowerInvariant();

            var cabecera = new byte[12];

            using var stream = imagen.OpenReadStream();
            var leidos = 0;

            while (leidos < cabecera.Length)
            {
                var cantidad = await stream.ReadAsync(
                    cabecera.AsMemory(leidos, cabecera.Length - leidos));

                if (cantidad == 0)
                    break;

                leidos += cantidad;
            }

            var jpeg = leidos >= 3 &&
                cabecera[0] == 0xFF &&
                cabecera[1] == 0xD8 &&
                cabecera[2] == 0xFF;

            var png = leidos >= 8 &&
                cabecera.Take(8).SequenceEqual(
                    new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

            var webp = leidos >= 12 &&
                System.Text.Encoding.ASCII.GetString(cabecera, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(cabecera, 8, 4) == "WEBP";

            var valida =
                ((extension == ".jpg" || extension == ".jpeg") &&
                    mime == "image/jpeg" && jpeg) ||
                (extension == ".png" && mime == "image/png" && png) ||
                (extension == ".webp" && mime == "image/webp" && webp);

            if (!valida)
            {
                ModelState.AddModelError(
                    "imagen",
                    "El archivo debe ser una imagen JPG, PNG o WEBP válida.");
            }
        }

        private async Task<string?> GuardarImagenAsync(IFormFile? imagen)
        {
            if (imagen == null)
                return null;

            var carpeta = Path.Combine(
                _environment.WebRootPath, "images", "perfumes");

            Directory.CreateDirectory(carpeta);

            var nombre = Guid.NewGuid().ToString("N") +
                Path.GetExtension(imagen.FileName).ToLowerInvariant();

            var rutaFisica = Path.Combine(carpeta, nombre);

            try
            {
                using var stream = new FileStream(
                    rutaFisica, FileMode.CreateNew);

                await imagen.CopyToAsync(stream);
            }
            catch
            {
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);

                throw;
            }

            return "/images/perfumes/" + nombre;
        }

        private void EliminarImagenNueva(string? ruta)
        {
            if (ruta == null)
                return;

            var rutaFisica = Path.Combine(
                _environment.WebRootPath,
                "images",
                "perfumes",
                Path.GetFileName(ruta));

            if (System.IO.File.Exists(rutaFisica))
                System.IO.File.Delete(rutaFisica);
        }
    }
}