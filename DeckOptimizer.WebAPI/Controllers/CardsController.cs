using DeckOptimizer.Domain.Entities;
using DeckOptimizer.Infrastructure.Services;
using DeckOptimizer.WebAPI.Contracts;
using DeckOptimizer.WebAPI.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace DeckOptimizer.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        private readonly CardService _cardService;

        public CardsController(CardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet]
        public ActionResult<List<CardDto>> GetCards()
        {
            return _cardService.GetAllCards()
                .Select(ApiMapping.ToDto)
                .ToList();
        }

        [HttpGet("{id:guid}")]
        public ActionResult<CardDto> GetCard(Guid id)
        {
            var card = _cardService.GetAllCards().FirstOrDefault(c => c.Id == id);
            return card == null ? NotFound() : ApiMapping.ToDto(card);
        }

        [HttpPost]
        public ActionResult<CardDto> CreateCard(CreateCardRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Название карты не может быть пустым.");
            }

            if (request.Cost < 0)
            {
                return BadRequest("Стоимость карты не может быть отрицательной.");
            }

            var characteristics = _cardService.EnsureDefaultCharacteristics();
            var unknownIds = GetUnknownCharacteristicIds(request.CharacteristicValues.Keys, characteristics);
            if (unknownIds.Count > 0)
            {
                return BadRequest($"Неизвестные характеристики: {string.Join(", ", unknownIds)}");
            }

            var card = new Card
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Cost = request.Cost
            };

            foreach (var characteristic in characteristics)
            {
                card.CharacteristicValues.Add(new CharacteristicValue
                {
                    CardId = card.Id,
                    CharacteristicId = characteristic.Id,
                    Value = request.CharacteristicValues.TryGetValue(characteristic.Id, out var value) ? value : 0
                });
            }

            _cardService.AddCard(card);

            var createdCard = _cardService.GetAllCards().First(c => c.Id == card.Id);
            return CreatedAtAction(nameof(GetCard), new { id = createdCard.Id }, ApiMapping.ToDto(createdCard));
        }

        [HttpPut("{id:guid}")]
        public ActionResult<CardDto> UpdateCard(Guid id, UpdateCardRequest request)
        {
            var card = _cardService.GetAllCards().FirstOrDefault(c => c.Id == id);
            if (card == null)
            {
                return NotFound();
            }

            if (request.Cost is < 0)
            {
                return BadRequest("Стоимость карты не может быть отрицательной.");
            }

            var characteristics = _cardService.EnsureDefaultCharacteristics();
            if (request.CharacteristicValues != null)
            {
                var unknownIds = GetUnknownCharacteristicIds(request.CharacteristicValues.Keys, characteristics);
                if (unknownIds.Count > 0)
                {
                    return BadRequest($"Неизвестные характеристики: {string.Join(", ", unknownIds)}");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                card.Name = request.Name.Trim();
            }

            if (request.Cost.HasValue)
            {
                card.Cost = request.Cost.Value;
            }

            if (request.CharacteristicValues != null)
            {
                foreach (var pair in request.CharacteristicValues)
                {
                    var value = card.CharacteristicValues.FirstOrDefault(cv => cv.CharacteristicId == pair.Key);
                    if (value == null)
                    {
                        value = new CharacteristicValue
                        {
                            CardId = card.Id,
                            CharacteristicId = pair.Key
                        };

                        card.CharacteristicValues.Add(value);
                    }

                    value.Value = pair.Value;
                }
            }

            _cardService.UpdateCard(card);

            var updatedCard = _cardService.GetAllCards().First(c => c.Id == id);
            return ApiMapping.ToDto(updatedCard);
        }

        [HttpDelete("{id:guid}")]
        public IActionResult DeleteCard(Guid id)
        {
            var card = _cardService.GetAllCards().FirstOrDefault(c => c.Id == id);
            if (card == null)
            {
                return NotFound();
            }

            _cardService.DeleteCard(id);
            return NoContent();
        }

        private static List<Guid> GetUnknownCharacteristicIds(IEnumerable<Guid> ids, IReadOnlyCollection<Characteristic> characteristics)
        {
            var knownIds = characteristics.Select(c => c.Id).ToHashSet();
            return ids.Where(id => !knownIds.Contains(id)).ToList();
        }
    }
}
