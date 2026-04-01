using DeckOptimizer.Application;
using DeckOptimizer.Domain.Entities;
using DeckOptimizer.Infrastructure;

namespace DeckOptimizer.UI
{
    public class CardGenerator
    {
        public static void SeedDatabase(AppDbContext dbContext)
        {
            //Очищаем базу
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            Console.WriteLine("Генерация свежего набора данных (20 карт)...");

            //Создаем базовые характеристики
            var attack = new Characteristic { Id = Guid.NewGuid(), Name = "Атака" };
            var health = new Characteristic { Id = Guid.NewGuid(), Name = "Здоровье" };

            dbContext.Characteristics.AddRange(attack, health);
            dbContext.SaveChanges();

            var rnd = new Random();
            var cards = new List<Card>();

            //Генерируем разнообразные карты
            for (int i = 1; i <= 20; i++)
            {
                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    Name = GetRandomName(rnd, i),
                    Cost = rnd.Next(1, 15) //ci в математической постановке
                };

                //Добавляем значения характеристик (x_ij)
                card.CharacteristicValues.Add(new CharacteristicValue
                {
                    CharacteristicId = attack.Id,
                    Value = rnd.Next(1, 20)
                });
                card.CharacteristicValues.Add(new CharacteristicValue
                {
                    CharacteristicId = health.Id,
                    Value = rnd.Next(1, 20)
                });

                cards.Add(card);
            }

            dbContext.Cards.AddRange(cards);
            dbContext.SaveChanges();
            Console.WriteLine("База данных успешно обновлена.");
        }

        private static string GetRandomName(Random rnd, int index)
        {
            string[] adj = { "Могучий", "Теневой", "Золотой", "Дикий", "Древний" };
            string[] noun = { "Воин", "Маг", "Зверь", "Артефакт", "Страж" };
            return $"{adj[rnd.Next(adj.Length)]} {noun[rnd.Next(noun.Length)]} #{index}";
        }
    }
}
