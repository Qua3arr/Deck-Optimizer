using DeckOptimizer.Application.Interfaces;
using DeckOptimizer.Application.Models;
using DeckOptimizer.Domain.Entities;
using System.Diagnostics;

namespace DeckOptimizer.Application.Services
{
    public class BruteForceOptimizer : IOptimizer
    {
        private const double Epsilon = 1e-9;

        private List<Card> _bestSolution = new();
        private double _bestValue = double.NegativeInfinity;
        private int _nodeCount;

        public OptimizationResult Optimize(OptimizationParameters parameters, IList<Card> cards)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(cards);

            _bestSolution = new List<Card>();
            _bestValue = double.NegativeInfinity;
            _nodeCount = 0;

            var watch = Stopwatch.StartNew();

            Search(
                index: 0,
                currentCost: 0,
                currentSize: 0,
                currentValue: 0,
                selected: new List<Card>(),
                cards: cards,
                parameters: parameters);

            watch.Stop();

            if (double.IsNegativeInfinity(_bestValue))
            {
                return new OptimizationResult
                {
                    HasSolution = false,
                    CalculationTime = watch.Elapsed,
                    VisitedNodeCount = _nodeCount
                };
            }

            return new OptimizationResult
            {
                HasSolution = true,
                SelectedCards = _bestSolution,
                TotalCost = _bestSolution.Sum(c => c.Cost),
                AggregatedValue = _bestValue,
                CalculationTime = watch.Elapsed,
                VisitedNodeCount = _nodeCount
            };
        }

        private void Search(
            int index,
            decimal currentCost,
            int currentSize,
            double currentValue,
            List<Card> selected,
            IList<Card> cards,
            OptimizationParameters parameters)
        {
            _nodeCount++;

            if (currentCost > parameters.MaxCost || currentSize > parameters.DeckSize)
            {
                return;
            }

            int cardsNeeded = parameters.DeckSize - currentSize;
            int remainingCards = cards.Count - index;

            if (remainingCards < cardsNeeded)
            {
                return;
            }

            if (currentSize == parameters.DeckSize)
            {
                if (currentValue > _bestValue + Epsilon)
                {
                    _bestValue = currentValue;
                    _bestSolution = new List<Card>(selected);
                }

                return;
            }

            if (index >= cards.Count)
            {
                return;
            }

            var card = cards[index];

            selected.Add(card);
            Search(
                index + 1,
                currentCost + card.Cost,
                currentSize + 1,
                currentValue + CalculateCardValue(card, parameters.Weights),
                selected,
                cards,
                parameters);
            selected.RemoveAt(selected.Count - 1);

            Search(
                index + 1,
                currentCost,
                currentSize,
                currentValue,
                selected,
                cards,
                parameters);
        }

        private static double CalculateCardValue(Card card, Dictionary<Guid, double> weights)
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
    }
}
