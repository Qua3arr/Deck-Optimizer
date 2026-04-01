using Microsoft.EntityFrameworkCore;
using DeckOptimizer.Infrastructure;
using DeckOptimizer.Application;
using DeckOptimizer.Application.Services;
using DeckOptimizer.Application.Models;
using DeckOptimizer.Domain.Entities;
using DotNetEnv;

Env.Load();

Console.WriteLine("=== DeckOptimizer v1.0 ===");

//Настройка контекста БД
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Строка подключения не найдена в .env файле!");
}
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString).UseLazyLoadingProxies();

using var dbContext = new AppDbContext(optionsBuilder.Options);

//Автоматически создаем базу, если её нет
dbContext.Database.EnsureCreated();

//Инициализация сервисов
var cardService = new CardService(dbContext);
var optimizer = new BranchAndBoundOptimizer();

//Добавление тестовых данных (если база пуста)
if (!dbContext.Cards.Any())
{
    Console.WriteLine("Наполнение базы тестовыми картами...");
    var attackId = Guid.NewGuid();
    var healthId = Guid.NewGuid();

    var characteristicAttack = new Characteristic { Id = attackId, Name = "Атака" };
    var characteristicHealth = new Characteristic { Id = healthId, Name = "Здоровье" };

    dbContext.Characteristics.AddRange(characteristicAttack, characteristicHealth);

    var card1 = new Card { Name = "Огненный элементаль", Cost = 5 };
    card1.CharacteristicValues.Add(new CharacteristicValue { Characteristic = characteristicAttack, Value = 10 });
    card1.CharacteristicValues.Add(new CharacteristicValue { Characteristic = characteristicHealth, Value = 5 });

    var card2 = new Card { Name = "Железный гном", Cost = 3 };
    card2.CharacteristicValues.Add(new CharacteristicValue { Characteristic = characteristicAttack, Value = 4 });
    card2.CharacteristicValues.Add(new CharacteristicValue { Characteristic = characteristicHealth, Value = 12 });

    cardService.AddCard(card1);
    cardService.AddCard(card2);
}

//Запуск оптимизации
var allCards = cardService.GetAllCards();
var parameters = new OptimizationParameters
{
    MaxCost = 10,  //Бюджет C
    DeckSize = 2,  //Количество карт K
    Weights = allCards.First().CharacteristicValues
        .ToDictionary(cv => cv.CharacteristicId, cv => 1.0) //Веса 1.0 для всех параметров
};

Console.WriteLine("Запуск алгоритма Branch and Bound...");
var result = optimizer.Optimize(parameters, allCards);

//Вывод результатов
Console.WriteLine("\n--- Оптимальная колода найдена! ---");
foreach (var card in result.SelectedCards)
{
    Console.WriteLine($"- {card.Name} (Цена: {card.Cost})");
}

Console.WriteLine($"\nИтоговая стоимость: {result.TotalCost}");
Console.WriteLine($"Общая ценность (F): {result.AggregatedValue:F2}");
Console.WriteLine($"Время расчета: {result.CalculationTime.TotalMilliseconds} мс");