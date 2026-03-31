using DeckOptimizer.Domain.Entities;

namespace DeckOptimizer.Application.Models
{
    internal class OptimizationResult
    {
        //Список оптимально подобранных карт 
        public List<Card> SelectedCards { get; set; } = new();

        //Итоговая стоимость 
        public decimal TotalCost { get; set; }

        //Агрегированный показатель качества F 
        public double AggregatedValue { get; set; }

        //Время, затраченное на вычисление 
        public TimeSpan CalculationTime { get; set; }
    }
}
