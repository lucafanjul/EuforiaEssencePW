using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Services
{
    public static class PaginacionAdmin
    {
        public static async Task<List<T>> CargarAsync<T>(
            IOrderedQueryable<T> consulta,
            int pagina,
            ViewDataDictionary vista)
        {
            const int porPagina = 15;

            var total = await consulta.CountAsync();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina));
            pagina = Math.Clamp(pagina, 1, totalPaginas);

            vista["TotalRegistros"] = total;
            vista["Pagina"] = pagina;
            vista["TotalPaginas"] = totalPaginas;

            return await consulta
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();
        }
    }
}