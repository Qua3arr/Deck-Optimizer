using DeckOptimizer.Application.Models;
using DeckOptimizer.Domain.Entities;

namespace DeckOptimizer.Application.Interfaces
{
    public interface IOptimizer
    {
        // Основной метод для запуска оптимизации
        OptimizationResult Optimize(OptimizationParameters parameters, IList<Card> cards);
    }
}
