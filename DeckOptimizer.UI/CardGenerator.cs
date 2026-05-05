using DeckOptimizer.Domain.Entities;
using DeckOptimizer.Infrastructure;

namespace DeckOptimizer.UI
{
    public class CardGenerator
    {
        public static void SeedDatabase(AppDbContext dbContext, bool resetDatabase = false, int cardCount = 20)
        {
            if (resetDatabase)
            {
                //Полный сброс нужен только для демонстрационного набора данных
                dbContext.ChangeTracker.Clear();
                dbContext.Database.EnsureDeleted();
            }

            dbContext.Database.EnsureCreated();

            if (!resetDatabase && dbContext.Cards.Any())
            {
                Console.WriteLine("В базе уже есть карты. Демо-набор не перезаписывается.");
                return;
            }

            Console.WriteLine($"Генерация демонстрационного набора данных ({cardCount} карт)...");

            var characteristics = dbContext.Characteristics
                .OrderBy(c => c.Name)
                .ToList();

            if (characteristics.Count == 0)
            {
                //Создаем базовые характеристики
                characteristics = new List<Characteristic>
                {
                    new() { Id = Guid.NewGuid(), Name = "Атака" },
                    new() { Id = Guid.NewGuid(), Name = "Здоровье" }
                };

                dbContext.Characteristics.AddRange(characteristics);
                dbContext.SaveChanges();
            }

            var rnd = new Random(1337);
            var cards = new List<Card>();

            //Генерируем разнообразные карты
            for (int i = 1; i <= cardCount; i++)
            {
                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    Name = GetRandomName(rnd, i),
                    Cost = rnd.Next(1, 15) //ci в математической постановке
                };

                foreach (var characteristic in characteristics)
                {
                    //Добавляем значения характеристик (x_ij)
                    card.CharacteristicValues.Add(new CharacteristicValue
                    {
                        CardId = card.Id,
                        CharacteristicId = characteristic.Id,
                        Value = rnd.Next(1, 20)
                    });
                }

                cards.Add(card);
            }

            dbContext.Cards.AddRange(cards);
            dbContext.SaveChanges();
            dbContext.ChangeTracker.Clear();
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
