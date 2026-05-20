using DeckOptimizer.Application.Models;
using DeckOptimizer.Application.Services;
using DeckOptimizer.Infrastructure.Services;
using DeckOptimizer.WebAPI.Contracts;
using DeckOptimizer.WebAPI.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace DeckOptimizer.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptimizationController : ControllerBase
    {
        private readonly CardService _cardService;
        private readonly BranchAndBoundOptimizer _optimizer;

        public OptimizationController(CardService cardService, BranchAndBoundOptimizer optimizer)
        {
            _cardService = cardService;
            _optimizer = optimizer;
        }

        [HttpPost]
        public ActionResult<OptimizationResponse> Optimize(OptimizeDeckRequest request)
        {
            if (request.MaxCost < 0)
            {
                return BadRequest("Максимальная стоимость не может быть отрицательной.");
            }

            if (request.DeckSize < 0)
            {
                return BadRequest("Размер колоды не может быть отрицательным.");
            }

            var cards = _cardService.GetAllCards();
            var knownCharacteristicIds = _cardService.GetAllCharacteristics().Select(c => c.Id).ToHashSet();
            var unknownIds = request.Weights.Keys.Where(id => !knownCharacteristicIds.Contains(id)).ToList();
            if (unknownIds.Count > 0)
            {
                return BadRequest($"Неизвестные характеристики: {string.Join(", ", unknownIds)}");
            }

            var parameters = new OptimizationParameters
            {
                MaxCost = request.MaxCost,
                DeckSize = request.DeckSize,
                Weights = request.Weights
            };

            var result = _optimizer.Optimize(parameters, cards);
            return ApiMapping.ToDto(result);
        }
    }
}
