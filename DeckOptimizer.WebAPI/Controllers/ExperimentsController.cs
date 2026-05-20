using DeckOptimizer.Application.Models;
using DeckOptimizer.Application.Services;
using DeckOptimizer.Domain.Entities;
using DeckOptimizer.Infrastructure.Services;
using DeckOptimizer.WebAPI.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DeckOptimizer.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExperimentsController : ControllerBase
    {
        private const long BruteForceCombinationLimit = 250_000;

        private readonly CardService _cardService;

        public ExperimentsController(CardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet]
        public ActionResult<List<ExperimentResultDto>> RunExperiments()
        {
            var characteristics = _cardService.GetAllCharacteristics();
            if (characteristics.Count == 0)
            {
                characteristics = new List<Characteristic>
                {
                    new() { Id = Guid.NewGuid(), Name = "Атака" },
                    new() { Id = Guid.NewGuid(), Name = "Здоровье" }
                };
            }

            var weights = characteristics.ToDictionary(c => c.Id, _ => 1.0);
            var cases = new[]
            {
                new ExperimentCase(10, 3),
                new ExperimentCase(15, 4),
                new ExperimentCase(20, 5),
                new ExperimentCase(30, 5),
                new ExperimentCase(50, 5),
                new ExperimentCase(100, 5)
            };

            return cases
                .Select(experimentCase => RunExperiment(experimentCase, characteristics, weights))
                .ToList();
        }

        private static ExperimentResultDto RunExperiment(
            ExperimentCase experimentCase,
            IReadOnlyList<Characteristic> characteristics,
            Dictionary<Guid, double> weights)
        {
            var cards = GenerateExperimentCards(experimentCase.CardCount, characteristics);
            var parameters = new OptimizationParameters
            {
                MaxCost = experimentCase.DeckSize * 8,
                DeckSize = experimentCase.DeckSize,
                Weights = weights
            };

            var branchAndBoundResult = new BranchAndBoundOptimizer().Optimize(parameters, cards);
            var combinationCount = CountCombinationsLimited(cards.Count, parameters.DeckSize, BruteForceCombinationLimit);

            var result = new ExperimentResultDto
            {
                CardCount = cards.Count,
                DeckSize = parameters.DeckSize,
                MaxCost = parameters.MaxCost,
                HasSolution = branchAndBoundResult.HasSolution,
                AggregatedValue = branchAndBoundResult.AggregatedValue,
                BranchAndBoundTimeMs = branchAndBoundResult.CalculationTime.TotalMilliseconds,
                BranchAndBoundVisitedNodes = branchAndBoundResult.VisitedNodeCount,
                CombinationCount = combinationCount > BruteForceCombinationLimit ? -1 : combinationCount,
                BruteForceWasRun = combinationCount <= BruteForceCombinationLimit
            };

            if (result.BruteForceWasRun)
            {
                var bruteForceResult = new BruteForceOptimizer().Optimize(parameters, cards);
                result.BruteForceTimeMs = bruteForceResult.CalculationTime.TotalMilliseconds;
                result.MatchesBruteForce = AreResultsEqual(branchAndBoundResult, bruteForceResult);
            }

            return result;
        }

        private static List<Card> GenerateExperimentCards(int cardCount, IReadOnlyList<Characteristic> characteristics)
        {
            var random = new Random(10_000 + cardCount * 31 + characteristics.Count);
            var cards = new List<Card>();

            for (int i = 1; i <= cardCount; i++)
            {
                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    Name = $"Experiment Card #{i}",
                    Cost = random.Next(1, 16)
                };

                foreach (var characteristic in characteristics)
                {
                    card.CharacteristicValues.Add(new CharacteristicValue
                    {
                        CardId = card.Id,
                        CharacteristicId = characteristic.Id,
                        Characteristic = characteristic,
                        Value = random.Next(1, 21)
                    });
                }

                cards.Add(card);
            }

            return cards;
        }

        private static long CountCombinationsLimited(int n, int k, long limit)
        {
            if (k < 0 || k > n)
            {
                return 0;
            }

            k = Math.Min(k, n - k);
            decimal result = 1;

            for (int i = 1; i <= k; i++)
            {
                result = result * (n - k + i) / i;
                if (result > limit)
                {
                    return limit + 1;
                }
            }

            return (long)result;
        }

        private static bool AreResultsEqual(OptimizationResult first, OptimizationResult second)
        {
            if (!first.HasSolution && !second.HasSolution)
            {
                return true;
            }

            return first.HasSolution == second.HasSolution &&
                Math.Abs(first.AggregatedValue - second.AggregatedValue) <= 1e-6;
        }

        private sealed record ExperimentCase(int CardCount, int DeckSize);
    }
}
