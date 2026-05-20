using DeckOptimizer.Application.Models;
using DeckOptimizer.Domain.Entities;
using DeckOptimizer.WebAPI.Contracts;

namespace DeckOptimizer.WebAPI.Mapping
{
    internal static class ApiMapping
    {
        public static CardDto ToDto(Card card)
        {
            return new CardDto
            {
                Id = card.Id,
                Name = card.Name,
                Cost = card.Cost,
                Characteristics = card.CharacteristicValues
                    .OrderBy(cv => cv.Characteristic?.Name)
                    .Select(cv => new CharacteristicValueDto
                    {
                        CharacteristicId = cv.CharacteristicId,
                        CharacteristicName = cv.Characteristic?.Name ?? cv.CharacteristicId.ToString(),
                        Value = cv.Value
                    })
                    .ToList()
            };
        }

        public static CharacteristicDto ToDto(Characteristic characteristic)
        {
            return new CharacteristicDto
            {
                Id = characteristic.Id,
                Name = characteristic.Name
            };
        }

        public static OptimizationResponse ToDto(OptimizationResult result)
        {
            return new OptimizationResponse
            {
                HasSolution = result.HasSolution,
                TotalCost = result.TotalCost,
                AggregatedValue = result.AggregatedValue,
                CalculationTimeMs = result.CalculationTime.TotalMilliseconds,
                VisitedNodeCount = result.VisitedNodeCount,
                SelectedCards = result.SelectedCards.Select(ToDto).ToList(),
                AggregatedCharacteristics = result.SelectedCards
                    .SelectMany(c => c.CharacteristicValues)
                    .GroupBy(cv => new
                    {
                        cv.CharacteristicId,
                        Name = cv.Characteristic?.Name ?? cv.CharacteristicId.ToString()
                    })
                    .Select(g => new CharacteristicAggregateDto
                    {
                        CharacteristicId = g.Key.CharacteristicId,
                        CharacteristicName = g.Key.Name,
                        Value = g.Sum(cv => cv.Value)
                    })
                    .OrderBy(x => x.CharacteristicName)
                    .ToList()
            };
        }
    }
}
