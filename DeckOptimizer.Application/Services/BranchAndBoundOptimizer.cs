using DeckOptimizer.Application.Models;
using DeckOptimizer.Application.Interfaces;
using DeckOptimizer.Domain.Entities;
using System.Diagnostics;

namespace DeckOptimizer.Application.Services
{
    public class BranchAndBoundOptimizer : IOptimizer
    {
        // Поля из UML-диаграммы для хранения лучшего найденного решения
        private List<Card> _bestSolution = new();
        private double _bestValue = double.MinValue;
        private int _nodeCount = 0;

        public OptimizationResult Optimize(OptimizationParameters parameters, IList<Card> cards)
        {
            _bestSolution.Clear();
            _bestValue = double.MinValue;
            _nodeCount = 0;

            var watch = Stopwatch.StartNew();

            // Здесь в будущих коммитах будет вызов BranchAndBoundRecursive

            watch.Stop();

            return new OptimizationResult
            {
                SelectedCards = _bestSolution,
                CalculationTime = watch.Elapsed,
                AggregatedValue = _bestValue
            };
        }

        // Рекурсивный метод для обхода дерева решений (по ТЗ раздел 5.1)
        private void BranchAndBoundRecursive(
            int index,
            decimal currentCost,
            int currentSize,
            double currentValue,
            List<Card> selected,
            IList<Card> allCards,
            OptimizationParameters parameters)
        {
            _nodeCount++;
            // Логика будет реализована в след. коммитах
        }

        // Оценочная функция для отсечения неэффективных ветвей
        private double EvaluateBound(int index, int currentSize, double currentValue, IList<Card> remainingCards, OptimizationParameters parameters)
        {
            return 0; // Будет реализовано позже
        }

        // Проверка ограничений (Макс. стоимость C и размер колоды K)
        private bool IsFeasible(decimal currentCost, int currentSize, OptimizationParameters parameters)
        {
            return currentCost <= parameters.MaxCost && currentSize <= parameters.DeckSize;
        }
    }
}
