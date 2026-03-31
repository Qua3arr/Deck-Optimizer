using DeckOptimizer.Domain.Entities;

namespace DeckOptimizer.Application.Models
{
    public class OptimizationParameters
    {
        //Максимальная суммарная стоимость C 
        public decimal MaxCost { get; set; }

        //Фиксированный размер колоды K 
        public int DeckSize { get; set; }

        //Весовые коэффициенты характеристик w_j 
        //Ключ — Id характеристики, значение — её вес
        public Dictionary<Guid, double> Weights { get; set; } = new();
    }
}
