# DeckOptimizer

Система на .NET 8 для хранения карт и оптимального подбора колоды при ограничениях на бюджет и размер.

## Возможности

- хранение карт в PostgreSQL через Entity Framework Core;
- просмотр, добавление, редактирование, удаление и фильтрация карт;
- добавление новых характеристик с заполнением значения по умолчанию для существующих карт;
- задание бюджета, размера колоды и весов характеристик;
- точный алгоритм Branch and Bound с безопасной верхней оценкой;
- вывод итоговой стоимости, значения целевой функции F, агрегированных характеристик, времени расчета и числа просмотренных узлов;
- экспериментальный режим для оценки времени работы и сравнения с полным перебором;
- автоматическая сверка с полным перебором на небольших наборах.

## Запуск

1. Укажите строку подключения в `DeckOptimizer.WebAPI/appsettings.json`, переменной окружения `DB_CONNECTION` или в уже существующем `DeckOptimizer.UI/.env`:

```env
DB_CONNECTION=Host=localhost;Database=deck_optimizer;Username=postgres;Password=postgres
```

2. Соберите проект:

```powershell
dotnet build Deck-Optimizer.sln -m:1 -nr:false
```

3. Запустите WebAPI:

```powershell
dotnet run --project DeckOptimizer.WebAPI
```

Swagger будет доступен по адресу, который выведет приложение, например:

```text
https://localhost:7000/swagger
```

Основные эндпоинты:

```text
GET    /api/cards
GET    /api/cards/{id}
POST   /api/cards
PUT    /api/cards/{id}
DELETE /api/cards/{id}

GET    /api/characteristics
POST   /api/characteristics

POST   /api/optimization
GET    /api/experiments
```

4. При необходимости можно запустить старое консольное приложение:

```powershell
dotnet run --project DeckOptimizer.UI
```

При первом запуске, если в базе нет карт, создается демонстрационный набор из 20 карт. Обычный запуск больше не удаляет существующие данные.

Для полного сброса и повторной генерации демо-данных:

```powershell
dotnet run --project DeckOptimizer.UI -- --reset-seed
```
