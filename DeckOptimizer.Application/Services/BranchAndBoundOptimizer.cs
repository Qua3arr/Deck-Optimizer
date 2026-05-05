using DeckOptimizer.Application.Models;
using DeckOptimizer.Application.Interfaces;
using DeckOptimizer.Domain.Entities;
using System.Diagnostics;

namespace DeckOptimizer.Application.Services
{
    public class BranchAndBoundOptimizer : IOptimizer
    {
        private const double Epsilon = 1e-9;

        private List<Card> _bestSolution = new();
        private double _bestValue = double.NegativeInfinity;
        private int _nodeCount = 0;

        public OptimizationResult Optimize(OptimizationParameters parameters, IList<Card> cards)
        {
            ValidateInput(parameters, cards);

            //Сброс состояния перед новым расчетом
            _bestSolution = new List<Card>();
            _bestValue = double.NegativeInfinity;
            _nodeCount = 0;

            var watch = Stopwatch.StartNew();

            if (parameters.DeckSize == 0)
            {
                watch.Stop();
                return new OptimizationResult
                {
                    HasSolution = true,
                    SelectedCards = new List<Card>(),
                    TotalCost = 0,
                    AggregatedValue = 0,
                    CalculationTime = watch.Elapsed,
                    VisitedNodeCount = 0
                };
            }

            if (cards.Count < parameters.DeckSize)
            {
                watch.Stop();
                return CreateNoSolutionResult(watch.Elapsed);
            }

            //Предварительная сортировка карт по удельной ценности помогает быстрее найти хорошее допустимое решение.
            //Корректность при этом обеспечивает только безопасная верхняя оценка в EvaluateBound.
            var sortedCards = cards
                .Select(c => new Candidate(c, CalculateCardValue(c, parameters.Weights)))
                .OrderByDescending(x => GetUnitValue(x))
                .ToList();

            //Запуск рекурсивного алгоритма с начальными параметрами
            BranchAndBoundRecursive(
                index: 0,
                currentCost: 0,
                currentSize: 0,
                currentValue: 0,
                selected: new List<Card>(),
                allCards: sortedCards,
                parameters: parameters);

            watch.Stop();

            if (double.IsNegativeInfinity(_bestValue))
            {
                return CreateNoSolutionResult(watch.Elapsed);
            }

            //Формирование итогового результата
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

        private void BranchAndBoundRecursive(
        int index, decimal currentCost, int currentSize, double currentValue,
        List<Card> selected, IList<Candidate> allCards, OptimizationParameters parameters)
        {
            //Увеличиваем счетчик узлов дерева для анализа сложности алгоритма
            _nodeCount++;

            int cardsNeeded = parameters.DeckSize - currentSize;

            //Базовый случай 1: Мы собрали нужное количество карт
            if (cardsNeeded == 0)
            {
                //Если текущая колода лучше лучшей найденной (и стоимость не превышена)
                if (currentValue > _bestValue + Epsilon && currentCost <= parameters.MaxCost)
                {
                    _bestValue = currentValue;
                    //Обязательно делаем копию списка, иначе по ссылке он очистится при возврате (backtracking)
                    _bestSolution = new List<Card>(selected);
                }
                return;
            }

            int remainingCount = allCards.Count - index;

            //Базовый случай 2: Карты закончились или их не хватит, чтобы добрать колоду
            if (index >= allCards.Count || remainingCount < cardsNeeded)
            {
                return;
            }

            //Если даже самые дешевые оставшиеся карты не помещаются в бюджет, ветвь недопустима
            if (!CanCompleteWithinBudget(index, currentCost, cardsNeeded, allCards, parameters.MaxCost))
            {
                return;
            }

            //ОТСЕЧЕНИЕ (Bounding): Вычисляем теоретический максимум для текущей ветви
            double bound = EvaluateBound(index, currentCost, currentSize, currentValue, allCards, parameters);

            //Если даже в идеальном случае мы не сможем побить текущий рекорд — отсекаем ветвь
            if (bound <= _bestValue + Epsilon)
            {
                return;
            }

            var currentCard = allCards[index];

            //ВЕТВЛЕНИЕ 1: Добавляем текущую карту в колоду
            //Проверяем, не нарушим ли мы ограничения по стоимости и размеру, добавив эту карту
            if (IsFeasible(currentCost + currentCard.Card.Cost, currentSize + 1, parameters))
            {
                selected.Add(currentCard.Card);

                //Идем глубже по дереву
                BranchAndBoundRecursive(
                    index + 1,
                    currentCost + currentCard.Card.Cost,
                    currentSize + 1,
                    currentValue + currentCard.Value,
                    selected,
                    allCards,
                    parameters);

                //ВОЗВРАТ (Backtracking): Убираем карту, чтобы рассмотреть другие варианты
                selected.RemoveAt(selected.Count - 1);
            }

            //ВЕТВЛЕНИЕ 2: Пропускаем текущую карту
            //Делаем небольшую оптимизацию: идем в эту ветвь, только если оставшихся карт хватит, чтобы добить колоду до нужного размера
            if (currentSize + (allCards.Count - index - 1) >= parameters.DeckSize)
            {
                BranchAndBoundRecursive(
                    index + 1,
                    currentCost,
                    currentSize,
                    currentValue,
                    selected,
                    allCards,
                    parameters);
            }
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
            IList<Candidate> remainingCards, OptimizationParameters parameters)
        {
            decimal costLeft = parameters.MaxCost - currentCost;
            int cardsNeeded = parameters.DeckSize - currentSize;

            //Если нам больше не нужно добавлять карты, текущая ценность и есть граница
            if (cardsNeeded <= 0) return currentValue;

            var remaining = remainingCards.Skip(index).ToList();

            //Верхняя оценка 1: берем самые ценные cardsNeeded карт, игнорируя стоимость.
            //Любая допустимая колода не может быть ценнее этой оценки.
            double topValuesBound = currentValue + remaining
                .OrderByDescending(x => x.Value)
                .Take(cardsNeeded)
                .Sum(x => x.Value);

            //Верхняя оценка 2: непрерывная релаксация рюкзака по бюджету без учета размера колоды.
            //Она тоже не ниже настоящего дискретного оптимума.
            double fractionalCostBound = currentValue;
            foreach (var item in remaining.OrderByDescending(GetUnitValue))
            {
                if (item.Value <= 0)
                {
                    break;
                }

                if (item.Card.Cost == 0)
                {
                    fractionalCostBound += item.Value;
                }
                else if (item.Card.Cost <= costLeft)
                {
                    //Если карта полностью помещается в остаток бюджета
                    fractionalCostBound += item.Value;
                    costLeft -= item.Card.Cost;
                }
                else
                {
                    //Непрерывная релаксация: берем "дробную" часть карты для расчета теоретического максимума
                    fractionalCostBound += item.Value * (double)(costLeft / item.Card.Cost);
                    break; //Дальше заполнять рюкзак нельзя, так как бюджет исчерпан
                }
            }

            return Math.Min(topValuesBound, fractionalCostBound);
        }

        private bool CanCompleteWithinBudget(
            int index,
            decimal currentCost,
            int cardsNeeded,
            IList<Candidate> remainingCards,
            decimal maxCost)
        {
            var cheapestAdditionalCost = remainingCards
                .Skip(index)
                .OrderBy(x => x.Card.Cost)
                .Take(cardsNeeded)
                .Sum(x => x.Card.Cost);

            return currentCost + cheapestAdditionalCost <= maxCost;
        }

        private static double GetUnitValue(Candidate candidate)
        {
            if (candidate.Card.Cost == 0)
            {
                return candidate.Value > 0 ? double.PositiveInfinity : candidate.Value;
            }

            return candidate.Value / (double)candidate.Card.Cost;
        }

        private static void ValidateInput(OptimizationParameters parameters, IList<Card> cards)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(cards);

            if (parameters.MaxCost < 0)
            {
                throw new ArgumentException("Максимальная стоимость не может быть отрицательной.");
            }

            if (parameters.DeckSize < 0)
            {
                throw new ArgumentException("Размер колоды не может быть отрицательным.");
            }

            if (cards.Any(c => c.Cost < 0))
            {
                throw new ArgumentException("Стоимость карты не может быть отрицательной.");
            }
        }

        private OptimizationResult CreateNoSolutionResult(TimeSpan calculationTime)
        {
            return new OptimizationResult
            {
                HasSolution = false,
                SelectedCards = new List<Card>(),
                TotalCost = 0,
                AggregatedValue = 0,
                CalculationTime = calculationTime,
                VisitedNodeCount = _nodeCount
            };
        }

        private sealed record Candidate(Card Card, double Value);
    }
}
