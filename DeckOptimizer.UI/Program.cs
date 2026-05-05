using System.Globalization;
using System.Text;
using DeckOptimizer.Application.Models;
using DeckOptimizer.Application.Services;
using DeckOptimizer.Domain.Entities;
using DeckOptimizer.Infrastructure;
using DeckOptimizer.Infrastructure.Services;
using DeckOptimizer.UI;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = Encoding.UTF8;
Env.Load();

Console.WriteLine("=== DeckOptimizer v1.1 ===");

//Настройка контекста БД
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Строка подключения не найдена в .env файле!");
}

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString).UseLazyLoadingProxies();

using var dbContext = new AppDbContext(optionsBuilder.Options);

dbContext.Database.EnsureCreated();

var resetSeed = args.Contains("--reset-seed", StringComparer.OrdinalIgnoreCase);
CardGenerator.SeedDatabase(dbContext, resetDatabase: resetSeed);

var cardService = new CardService(dbContext);

while (true)
{
    PrintMenu();
    var command = Console.ReadLine()?.Trim();

    try
    {
        switch (command)
        {
            case "1":
                PrintCards(cardService.GetAllCards());
                break;
            case "2":
                AddCard(cardService);
                break;
            case "3":
                EditCard(cardService);
                break;
            case "4":
                DeleteCard(cardService);
                break;
            case "5":
                FilterCards(cardService);
                break;
            case "6":
                RunOptimization(cardService);
                break;
            case "7":
                AddCharacteristic(cardService);
                break;
            case "8":
                CardGenerator.SeedDatabase(dbContext, resetDatabase: true);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Неизвестная команда.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

static void PrintMenu()
{
    Console.WriteLine();
    Console.WriteLine("1 - Показать карты");
    Console.WriteLine("2 - Добавить карту");
    Console.WriteLine("3 - Редактировать карту");
    Console.WriteLine("4 - Удалить карту");
    Console.WriteLine("5 - Фильтровать карты");
    Console.WriteLine("6 - Оптимизировать колоду");
    Console.WriteLine("7 - Добавить характеристику");
    Console.WriteLine("8 - Сбросить и заново создать демо-данные");
    Console.WriteLine("0 - Выход");
    Console.Write("Команда: ");
}

static void AddCard(CardService cardService)
{
    var characteristics = cardService.EnsureDefaultCharacteristics();

    var card = new Card
    {
        Id = Guid.NewGuid(),
        Name = ReadRequiredText("Название карты"),
        Cost = ReadDecimal("Стоимость карты", min: 0)
    };

    foreach (var characteristic in characteristics)
    {
        card.CharacteristicValues.Add(new CharacteristicValue
        {
            CardId = card.Id,
            CharacteristicId = characteristic.Id,
            Value = ReadDouble($"Значение характеристики \"{characteristic.Name}\"", min: 0)
        });
    }

    cardService.AddCard(card);
    Console.WriteLine("Карта добавлена.");
}

static void EditCard(CardService cardService)
{
    var card = SelectCard(cardService, "Номер карты для редактирования");
    if (card == null)
    {
        return;
    }

    var newName = ReadOptionalText($"Название [{card.Name}]");
    if (!string.IsNullOrWhiteSpace(newName))
    {
        card.Name = newName;
    }

    var newCost = ReadOptionalDecimal($"Стоимость [{card.Cost}]", min: 0);
    if (newCost.HasValue)
    {
        card.Cost = newCost.Value;
    }

    var characteristics = cardService.EnsureDefaultCharacteristics();
    foreach (var characteristic in characteristics)
    {
        var value = card.CharacteristicValues
            .FirstOrDefault(cv => cv.CharacteristicId == characteristic.Id);

        if (value == null)
        {
            value = new CharacteristicValue
            {
                CardId = card.Id,
                CharacteristicId = characteristic.Id,
                Value = 0
            };

            card.CharacteristicValues.Add(value);
        }

        var newValue = ReadOptionalDouble($"Значение \"{characteristic.Name}\" [{value.Value}]", min: 0);
        if (newValue.HasValue)
        {
            value.Value = newValue.Value;
        }
    }

    cardService.UpdateCard(card);
    Console.WriteLine("Карта обновлена.");
}

static void DeleteCard(CardService cardService)
{
    var card = SelectCard(cardService, "Номер карты для удаления");
    if (card == null)
    {
        return;
    }

    var confirm = ReadOptionalText($"Введите y, чтобы удалить \"{card.Name}\"");
    if (!string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Удаление отменено.");
        return;
    }

    cardService.DeleteCard(card.Id);
    Console.WriteLine("Карта удалена.");
}

static void AddCharacteristic(CardService cardService)
{
    var name = ReadRequiredText("Название новой характеристики");
    var defaultValue = ReadDoubleWithDefault("Значение для уже существующих карт", defaultValue: 0, min: 0);

    var characteristic = cardService.AddCharacteristic(name, defaultValue);
    Console.WriteLine($"Характеристика \"{characteristic.Name}\" добавлена.");
}

static void FilterCards(CardService cardService)
{
    var namePart = ReadOptionalText("Часть названия (Enter - без фильтра)");
    var maxCost = ReadOptionalDecimal("Максимальная стоимость (Enter - без ограничения)", min: 0);

    var characteristics = cardService.GetAllCharacteristics();
    Guid? characteristicId = null;
    double? minCharacteristicValue = null;

    if (characteristics.Count > 0)
    {
        Console.WriteLine("Фильтр по характеристике:");
        Console.WriteLine("0 - не использовать");
        for (int i = 0; i < characteristics.Count; i++)
        {
            Console.WriteLine($"{i + 1} - {characteristics[i].Name}");
        }

        var characteristicNumber = ReadInt("Номер характеристики", min: 0, max: characteristics.Count);
        if (characteristicNumber > 0)
        {
            var characteristic = characteristics[characteristicNumber - 1];
            characteristicId = characteristic.Id;
            minCharacteristicValue = ReadDouble($"Минимальное значение \"{characteristic.Name}\"", min: 0);
        }
    }

    var filteredCards = cardService.FilterCards(card =>
        (string.IsNullOrWhiteSpace(namePart) || card.Name.Contains(namePart, StringComparison.OrdinalIgnoreCase)) &&
        (!maxCost.HasValue || card.Cost <= maxCost.Value) &&
        (!characteristicId.HasValue || card.CharacteristicValues.Any(cv =>
            cv.CharacteristicId == characteristicId.Value &&
            cv.Value >= minCharacteristicValue.GetValueOrDefault())));

    PrintCards(filteredCards);
}

static void RunOptimization(CardService cardService)
{
    var allCards = cardService.GetAllCards();
    if (allCards.Count == 0)
    {
        Console.WriteLine("В базе нет карт.");
        return;
    }

    var characteristics = cardService.EnsureDefaultCharacteristics();
    var parameters = new OptimizationParameters
    {
        MaxCost = ReadDecimal("Максимальная суммарная стоимость C", min: 0),
        DeckSize = ReadInt($"Размер колоды K (1..{allCards.Count})", min: 1, max: allCards.Count),
        Weights = ReadWeights(characteristics)
    };

    Console.WriteLine("Запуск алгоритма Branch and Bound...");
    var optimizer = new BranchAndBoundOptimizer();
    var result = optimizer.Optimize(parameters, allCards);

    PrintOptimizationResult(result);
    RunBruteForceCheck(parameters, allCards, result);
}

static Dictionary<Guid, double> ReadWeights(IReadOnlyList<Characteristic> characteristics)
{
    var weights = new Dictionary<Guid, double>();

    foreach (var characteristic in characteristics)
    {
        weights[characteristic.Id] = ReadDoubleWithDefault(
            $"Вес характеристики \"{characteristic.Name}\"",
            defaultValue: 1,
            min: 0);
    }

    return weights;
}

static void RunBruteForceCheck(
    OptimizationParameters parameters,
    IList<Card> cards,
    OptimizationResult branchAndBoundResult)
{
    const long bruteForceCombinationLimit = 250_000;

    var combinationCount = CountCombinationsLimited(cards.Count, parameters.DeckSize, bruteForceCombinationLimit);
    if (combinationCount > bruteForceCombinationLimit)
    {
        Console.WriteLine($"Проверка полным перебором пропущена: комбинаций больше {bruteForceCombinationLimit}.");
        return;
    }

    var bruteForceResult = new BruteForceOptimizer().Optimize(parameters, cards);

    if (!branchAndBoundResult.HasSolution && !bruteForceResult.HasSolution)
    {
        Console.WriteLine("Проверка полным перебором: решений нет, результаты совпали.");
        return;
    }

    if (branchAndBoundResult.HasSolution != bruteForceResult.HasSolution ||
        Math.Abs(branchAndBoundResult.AggregatedValue - bruteForceResult.AggregatedValue) > 1e-6)
    {
        Console.WriteLine("ВНИМАНИЕ: результат Branch and Bound не совпал с полным перебором.");
        Console.WriteLine($"B&B F = {branchAndBoundResult.AggregatedValue:F2}; полный перебор F = {bruteForceResult.AggregatedValue:F2}");
        return;
    }

    Console.WriteLine(
        $"Проверка полным перебором: совпало ({combinationCount} комбинаций, {bruteForceResult.CalculationTime.TotalMilliseconds:F0} мс).");
}

static long CountCombinationsLimited(int n, int k, long limit)
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

static void PrintOptimizationResult(OptimizationResult result)
{
    if (!result.HasSolution)
    {
        Console.WriteLine("Решение не найдено: невозможно собрать колоду с такими ограничениями.");
        Console.WriteLine($"Время расчета: {result.CalculationTime.TotalMilliseconds:F0} мс.");
        Console.WriteLine($"Просмотрено узлов дерева: {result.VisitedNodeCount}");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("--- Оптимальная колода найдена ---");
    PrintCards(result.SelectedCards);

    Console.WriteLine($"Итоговая стоимость: {result.TotalCost}");
    Console.WriteLine($"Общая ценность F: {result.AggregatedValue:F2}");
    Console.WriteLine($"Время расчета: {result.CalculationTime.TotalMilliseconds:F0} мс.");
    Console.WriteLine($"Просмотрено узлов дерева: {result.VisitedNodeCount}");

    var aggregates = result.SelectedCards
        .SelectMany(c => c.CharacteristicValues)
        .GroupBy(cv => new
        {
            cv.CharacteristicId,
            Name = cv.Characteristic?.Name ?? cv.CharacteristicId.ToString()
        })
        .Select(g => new
        {
            g.Key.Name,
            Value = g.Sum(cv => cv.Value)
        })
        .OrderBy(x => x.Name)
        .ToList();

    Console.WriteLine("Итоговые агрегированные характеристики:");
    foreach (var aggregate in aggregates)
    {
        Console.WriteLine($"- {aggregate.Name}: {aggregate.Value:F2}");
    }
}

static Card? SelectCard(CardService cardService, string prompt)
{
    var cards = cardService.GetAllCards();
    if (cards.Count == 0)
    {
        Console.WriteLine("В базе нет карт.");
        return null;
    }

    PrintCards(cards);
    Console.WriteLine("0. Отмена");

    var number = ReadInt(prompt, min: 0, max: cards.Count);
    return number == 0 ? null : cards[number - 1];
}

static void PrintCards(IReadOnlyList<Card> cards)
{
    if (cards.Count == 0)
    {
        Console.WriteLine("Список карт пуст.");
        return;
    }

    Console.WriteLine();
    for (int i = 0; i < cards.Count; i++)
    {
        var card = cards[i];
        var values = card.CharacteristicValues
            .OrderBy(cv => cv.Characteristic?.Name)
            .Select(cv => $"{cv.Characteristic?.Name ?? cv.CharacteristicId.ToString()}: {cv.Value:F2}");

        Console.WriteLine($"{i + 1}. {card.Name} | Цена: {card.Cost} | {string.Join(", ", values)}");
    }
}

static string ReadRequiredText(string prompt)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        Console.WriteLine("Значение не может быть пустым.");
    }
}

static string? ReadOptionalText(string prompt)
{
    Console.Write($"{prompt}: ");
    var input = Console.ReadLine()?.Trim();
    return string.IsNullOrWhiteSpace(input) ? null : input;
}

static int ReadInt(string prompt, int? min = null, int? max = null)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine()?.Trim();

        if (int.TryParse(input, out var value) &&
            (!min.HasValue || value >= min.Value) &&
            (!max.HasValue || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine("Введите корректное целое число.");
    }
}

static decimal ReadDecimal(string prompt, decimal? min = null, decimal? max = null)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine()?.Trim();

        if (TryParseDecimal(input, out var value) &&
            (!min.HasValue || value >= min.Value) &&
            (!max.HasValue || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine("Введите корректное число.");
    }
}

static decimal? ReadOptionalDecimal(string prompt, decimal? min = null, decimal? max = null)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (TryParseDecimal(input, out var value) &&
            (!min.HasValue || value >= min.Value) &&
            (!max.HasValue || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine("Введите корректное число или оставьте поле пустым.");
    }
}

static double ReadDouble(string prompt, double? min = null, double? max = null)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine()?.Trim();

        if (TryParseDouble(input, out var value) &&
            (!min.HasValue || value >= min.Value) &&
            (!max.HasValue || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine("Введите корректное число.");
    }
}

static double ReadDoubleWithDefault(string prompt, double defaultValue, double? min = null, double? max = null)
{
    while (true)
    {
        Console.Write($"{prompt} [{defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        if (TryParseDouble(input, out var value) &&
            (!min.HasValue || value >= min.Value) &&
            (!max.HasValue || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine("Введите корректное число или оставьте поле пустым.");
    }
}

static double? ReadOptionalDouble(string prompt, double? min = null, double? max = null)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (TryParseDouble(input, out var value) &&
            (!min.HasValue || value >= min.Value) &&
            (!max.HasValue || value <= max.Value))
        {
            return value;
        }

        Console.WriteLine("Введите корректное число или оставьте поле пустым.");
    }
}

static bool TryParseDecimal(string? input, out decimal value)
{
    if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
    {
        return true;
    }

    return decimal.TryParse(input?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}

static bool TryParseDouble(string? input, out double value)
{
    if (double.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
    {
        return true;
    }

    return double.TryParse(input?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
