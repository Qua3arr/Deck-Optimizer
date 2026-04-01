using DeckOptimizer.Application.Models;
using DeckOptimizer.Application.Interfaces;
using DeckOptimizer.Domain.Entities;
using System.Diagnostics;

namespace DeckOptimizer.Application.Services
{
    public class BranchAndBoundOptimizer : IOptimizer
    {
        private List<Card> _bestSolution = new();
        private double _bestValue = double.MinValue;
        private int _nodeCount = 0;

        public OptimizationResult Optimize(OptimizationParameters parameters, IList<Card> cards)
        {
            return new OptimizationResult();
        }

        private void BranchAndBoundRecursive(
            int index, decimal currentCost, int currentSize, double currentValue,
            List<Card> selected, IList<Card> allCards, OptimizationParameters parameters)
        {
            
        }

        //Расчет агрегированного показателя качества F карты с учетом весов
        private double CalculateCardValue(Card card, Dictionary<Guid, double> weights)
        {
            double value = 0;
            foreach (var cv in card.CharacteristicValues)
            {
                if (weights.TryGetValue(cv.CharacteristicId, out var weight))
                {
                    value += cv.Value * weight;
                }
            }
            return value;
        }

        //Проверка ограничений (стоимость C и размер K)
        private bool IsFeasible(decimal currentCost, int currentSize, OptimizationParameters parameters)
        {
            return currentCost <= parameters.MaxCost && currentSize <= parameters.DeckSize;
        }

        //Оценочная функция для расчета верхней границы качества оставшихся карт 
        private double EvaluateBound(
            int index, decimal currentCost, int currentSize, double currentValue,
            IList<Card> remainingCards, OptimizationParameters parameters)
        {
            double bound = currentValue;
            decimal costLeft = parameters.MaxCost - currentCost;
            int cardsNeeded = parameters.DeckSize - currentSize;

            //Если нам больше не нужно добавлять карты, текущая ценность и есть граница
            if (cardsNeeded <= 0) return bound;

            //Жадная стратегия: рассчитываем удельную полезность (Ценность / Стоимость)
            var sortedRemaining = remainingCards.Skip(index)
                .Select(c => new
                {
                    Card = c,
                    Value = CalculateCardValue(c, parameters.Weights)
                })
                //Для карт с нулевой стоимостью используем малую константу во избежание деления на ноль
                .OrderByDescending(x => x.Card.Cost > 0 ? (decimal)x.Value / x.Card.Cost : (decimal)x.Value / 0.01m)
                .ToList();

            foreach (var item in sortedRemaining)
            {
                if (cardsNeeded == 0) break;

                if (item.Card.Cost <= costLeft)
                {
                    //Если карта полностью помещается в остаток бюджета
                    bound += item.Value;
                    costLeft -= item.Card.Cost;
                    cardsNeeded--;
                }
                else
                {
                    //Непрерывная релаксация: берем "дробную" часть карты для расчета теоретического максимума
                    bound += item.Value * (double)(costLeft / item.Card.Cost);
                    break; //Дальше заполнять рюкзак нельзя, так как бюджет исчерпан
                }
            }

            return bound;
        }
    }
}
