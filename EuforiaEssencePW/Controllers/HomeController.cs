using System.Diagnostics;
using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using EuforiaEssencePW.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var hoy = DateTime.UtcNow.AddHours(-3).Date;

            var campania = await _context.Campanias
                .AsNoTracking()
                .Where(c =>
                    c.Activa &&
                    c.Desde <= hoy &&
                    c.Hasta >= hoy &&
                    c.Productos.Any(cp => cp.Perfume.Activo))
                .OrderByDescending(c => c.Prioridad)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var seleccion = new List<Perfume>();

            if (campania != null)
            {
                var campaniaId = campania.Id;

                seleccion = await _context.Perfumes
                    .AsNoTracking()
                    .Where(p =>
                        p.Activo &&
                        _context.CampaniasPerfumes.Any(cp =>
                            cp.CampaniaId == campaniaId &&
                            cp.PerfumeId == p.Id))
                    .Include(p => p.Marca)
                    .Include(p => p.Presentaciones)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync(cancellationToken);
            }

            var novedades = await _context.Perfumes
                .AsNoTracking()
                .Where(p => p.Activo)
                .Include(p => p.Marca)
                .Include(p => p.Presentaciones)
                .OrderByDescending(p => p.FechaCreacion)
                .ThenByDescending(p => p.Id)
                .Take(6)
                .ToListAsync(cancellationToken);

            var categorias = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Perfumes.Any(p => p.Activo))
                .OrderBy(c => c.Nombre)
                .ToListAsync(cancellationToken);

            return View(new InicioViewModel
            {
                Campania = campania,
                Seleccion = seleccion,
                Novedades = novedades,
                Categorias = categorias
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}