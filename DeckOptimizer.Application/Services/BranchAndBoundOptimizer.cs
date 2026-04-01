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
            //Сброс состояния перед новым расчетом
            _bestSolution = new List<Card>();
            _bestValue = double.MinValue;
            _nodeCount = 0;

            //Предварительная сортировка карт по удельной ценности (Value / Cost)
            //Это критически важно для эффективности отсечений (Bounding)
            var sortedCards = cards
                .Select(c => new { Card = c, UnitValue = c.Cost > 0 ? CalculateCardValue(c, parameters.Weights) / (double)c.Cost : CalculateCardValue(c, parameters.Weights) / 0.01 })
                .OrderByDescending(x => x.UnitValue)
                .Select(x => x.Card)
                .ToList();

            var watch = Stopwatch.StartNew();

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

            //Формирование итогового результата
            return new OptimizationResult
            {
                SelectedCards = _bestSolution,
                TotalCost = _bestSolution.Sum(c => c.Cost),
                AggregatedValue = _bestValue,
                CalculationTime = watch.Elapsed
            };
        }

        private void BranchAndBoundRecursive(
        int index, decimal currentCost, int currentSize, double currentValue,
        List<Card> selected, IList<Card> allCards, OptimizationParameters parameters)
        {
            //Увеличиваем счетчик узлов дерева для анализа сложности алгоритма
            _nodeCount++;

            //Базовый случай 1: Мы собрали нужное количество карт
            if (currentSize == parameters.DeckSize)
            {
                //Если текущая колода лучше лучшей найденной (и стоимость не превышена)
                if (currentValue > _bestValue && currentCost <= parameters.MaxCost)
                {
                    _bestValue = currentValue;
                    //Обязательно делаем копию списка, иначе по ссылке он очистится при возврате (backtracking)
                    _bestSolution = new List<Card>(selected);
                }
                return;
            }

            //Базовый случай 2: Карты закончились, а колода не собрана
            if (index >= allCards.Count)
            {
                return;
            }

            //ОТСЕЧЕНИЕ (Bounding): Вычисляем теоретический максимум для текущей ветви
            double bound = EvaluateBound(index, currentCost, currentSize, currentValue, allCards, parameters);

            //Если даже в идеальном случае мы не сможем побить текущий рекорд — отсекаем ветвь
            if (bound <= _bestValue)
            {
                return;
            }

            var currentCard = allCards[index];

            //ВЕТВЛЕНИЕ 1: Добавляем текущую карту в колоду
            //Проверяем, не нарушим ли мы ограничения по стоимости и размеру, добавив эту карту
            if (IsFeasible(currentCost + currentCard.Cost, currentSize + 1, parameters))
            {
                selected.Add(currentCard);
                double cardValue = CalculateCardValue(currentCard, parameters.Weights);

                //Идем глубже по дереву
                BranchAndBoundRecursive(
                    index + 1,
                    currentCost + currentCard.Cost,
                    currentSize + 1,
                    currentValue + cardValue,
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
