using DeckOptimizer.Domain.Entities;

namespace DeckOptimizer.Application.Models
{
    public class OptimizationResult
    {
        //Флаг показывает, удалось ли найти колоду, удовлетворяющую ограничениям
        public bool HasSolution { get; set; }

        //Список оптимально подобранных карт 
        public List<Card> SelectedCards { get; set; } = new();

        //Итоговая стоимость 
        public decimal TotalCost { get; set; }

        //Агрегированный показатель качества F 
        public double AggregatedValue { get; set; }

        //Время, затраченное на вычисление 
        public TimeSpan CalculationTime { get; set; }

        //Количество просмотренных узлов дерева решений
        public int VisitedNodeCount { get; set; }
    }
}
