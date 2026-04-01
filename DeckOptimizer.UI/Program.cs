using Microsoft.EntityFrameworkCore;
using DeckOptimizer.Infrastructure;
using DeckOptimizer.Application;
using DeckOptimizer.Application.Services;
using DeckOptimizer.Application.Models;
using DeckOptimizer.Domain.Entities;
using DotNetEnv;
using DeckOptimizer.UI;

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

//Генерация карт
CardGenerator.SeedDatabase(dbContext);

//Инициализация сервисов
var cardService = new CardService(dbContext);
var optimizer = new BranchAndBoundOptimizer();

//Запуск оптимизации
var allCards = cardService.GetAllCards();
var parameters = new OptimizationParameters
{
    MaxCost = 30,  //Бюджет C
    DeckSize = 5,  //Количество карт K
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
Console.WriteLine($"Время расчета: {result.CalculationTime.TotalSeconds} сек.");