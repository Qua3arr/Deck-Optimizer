using DeckOptimizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeckOptimizer.Infrastructure.Services
{
    public class CardService
    {
        private readonly AppDbContext _dbContext;

        public CardService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        //Добавление карты 
        public void AddCard(Card card)
        {
            _dbContext.Cards.Add(card);
            _dbContext.SaveChanges();
        }

        //Редактирование характеристик и данных карты 
        public void UpdateCard(Card card)
        {
            if (_dbContext.Entry(card).State == EntityState.Detached)
            {
                _dbContext.Cards.Update(card);
            }

            _dbContext.SaveChanges();
        }

        //Удаление карты 
        public void DeleteCard(Guid cardId)
        {
            var card = _dbContext.Cards.Find(cardId);
            if (card != null)
            {
                _dbContext.Cards.Remove(card);
                _dbContext.SaveChanges();
            }
        }

        //Просмотр полного списка карт с их характеристиками 
        public List<Card> GetAllCards()
        {
            return _dbContext.Cards
                .Include(c => c.CharacteristicValues)
                .ThenInclude(cv => cv.Characteristic)
                .OrderBy(c => c.Name)
                .ToList();
        }

        //Просмотр полного списка характеристик
        public List<Characteristic> GetAllCharacteristics()
        {
            return _dbContext.Characteristics
                .OrderBy(c => c.Name)
                .ToList();
        }

        public List<Characteristic> EnsureDefaultCharacteristics()
        {
            var characteristics = GetAllCharacteristics();
            if (characteristics.Count > 0)
            {
                return characteristics;
            }

            characteristics = new List<Characteristic>
            {
                new() { Name = "Атака" },
                new() { Name = "Здоровье" }
            };

            _dbContext.Characteristics.AddRange(characteristics);
            _dbContext.SaveChanges();

            return GetAllCharacteristics();
        }

        public Characteristic AddCharacteristic(string name, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Название характеристики не может быть пустым.");
            }

            var trimmedName = name.Trim();
            if (GetAllCharacteristics().Any(c => string.Equals(c.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Характеристика с таким названием уже существует.");
            }

            using var transaction = _dbContext.Database.BeginTransaction();

            var characteristic = new Characteristic
            {
                Id = Guid.NewGuid(),
                Name = trimmedName
            };

            _dbContext.Characteristics.Add(characteristic);
            _dbContext.SaveChanges();

            foreach (var card in _dbContext.Cards.ToList())
            {
                _dbContext.CharacteristicValues.Add(new CharacteristicValue
                {
                    CardId = card.Id,
                    CharacteristicId = characteristic.Id,
                    Value = defaultValue
                });
            }

            _dbContext.SaveChanges();
            transaction.Commit();

            return characteristic;
        }

        //Фильтрация карт по заданному условию 
        public List<Card> FilterCards(Func<Card, bool> predicate)
        {
            return GetAllCards().Where(predicate).ToList();
        }
    }
}
