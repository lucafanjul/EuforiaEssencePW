using EuforiaEssencePW.Data;
using EuforiaEssencePW.Models;
using Microsoft.EntityFrameworkCore;

namespace EuforiaEssencePW.Services
{
    public static class StockPedidoService
    {
        public static async Task LiberarAsync(
            ApplicationDbContext context,
            Pedido pedido)
        {
            if (pedido.StockLiberado)
                return;

            var cantidades = CantidadesPorPresentacion(pedido);
            var presentaciones = await CargarPresentacionesAsync(context, cantidades);

            foreach (var item in cantidades)
            {
                if (!presentaciones.TryGetValue(item.Key, out var presentacion))
                {
                    throw new InvalidOperationException(
                        "No se encontró una presentación del pedido.");
                }

                presentacion.Stock = checked(presentacion.Stock + item.Value);
            }

            pedido.StockLiberado = true;
        }

        public static async Task<bool> IntentarReservarAsync(
            ApplicationDbContext context,
            Pedido pedido)
        {
            if (!pedido.StockLiberado)
                return true;

            var cantidades = CantidadesPorPresentacion(pedido);

            if (cantidades.Count == 0)
                return false;

            var presentaciones = await CargarPresentacionesAsync(context, cantidades);

            foreach (var item in cantidades)
            {
                if (!presentaciones.TryGetValue(item.Key, out var presentacion) ||
                    !presentacion.Activo ||
                    presentacion.Perfume?.Activo != true ||
                    presentacion.Stock < item.Value)
                {
                    return false;
                }
            }

            foreach (var item in cantidades)
                presentaciones[item.Key].Stock -= item.Value;

            pedido.StockLiberado = false;
            return true;
        }

        private static Dictionary<int, int> CantidadesPorPresentacion(Pedido pedido)
        {
            return pedido.Detalles
                .GroupBy(detalle => detalle.PresentacionId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Sum(detalle => detalle.Cantidad));
        }

        private static Task<Dictionary<int, Presentacion>> CargarPresentacionesAsync(
            ApplicationDbContext context,
            Dictionary<int, int> cantidades)
        {
            var ids = cantidades.Keys.ToList();

            return context.Presentaciones
                .Include(presentacion => presentacion.Perfume)
                .Where(presentacion => ids.Contains(presentacion.Id))
                .OrderBy(presentacion => presentacion.Id)
                .ToDictionaryAsync(presentacion => presentacion.Id);
        }
    }
}