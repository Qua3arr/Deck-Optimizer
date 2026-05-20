using DeckOptimizer.Infrastructure.Services;
using DeckOptimizer.WebAPI.Contracts;
using DeckOptimizer.WebAPI.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace DeckOptimizer.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharacteristicsController : ControllerBase
    {
        private readonly CardService _cardService;

        public CharacteristicsController(CardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet]
        public ActionResult<List<CharacteristicDto>> GetCharacteristics()
        {
            return _cardService.GetAllCharacteristics()
                .Select(ApiMapping.ToDto)
                .ToList();
        }

        [HttpGet("{id:guid}")]
        public ActionResult<CharacteristicDto> GetCharacteristic(Guid id)
        {
            var characteristic = _cardService.GetAllCharacteristics().FirstOrDefault(c => c.Id == id);
            return characteristic == null ? NotFound() : ApiMapping.ToDto(characteristic);
        }

        [HttpPost]
        public ActionResult<CharacteristicDto> CreateCharacteristic(CreateCharacteristicRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Название характеристики не может быть пустым.");
            }

            if (request.DefaultValue < 0)
            {
                return BadRequest("Значение характеристики не может быть отрицательным.");
            }

            try
            {
                var characteristic = _cardService.AddCharacteristic(request.Name, request.DefaultValue);
                return CreatedAtAction(nameof(GetCharacteristic), new { id = characteristic.Id }, ApiMapping.ToDto(characteristic));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
