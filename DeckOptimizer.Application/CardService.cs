using Microsoft.EntityFrameworkCore;
using DeckOptimizer.Domain.Entities;
using DeckOptimizer.Infrastructure;

namespace DeckOptimizer.Application
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
            _dbContext.Entry(card).State = EntityState.Modified;
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
                .ToList();
        }

        //Фильтрация карт по заданному условию 
        public List<Card> FilterCards(Func<Card, bool> predicate)
        {
            return GetAllCards().Where(predicate).ToList();
        }
    }
}
