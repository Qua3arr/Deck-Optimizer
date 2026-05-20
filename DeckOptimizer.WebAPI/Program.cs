using DeckOptimizer.Application.Services;
using DeckOptimizer.Infrastructure;
using DeckOptimizer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = FirstNonEmpty(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Configuration["DB_CONNECTION"],
    Environment.GetEnvironmentVariable("DB_CONNECTION"),
    ReadConnectionStringFromEnvFiles(builder.Environment.ContentRootPath));

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Строка подключения не найдена. Укажите ConnectionStrings:DefaultConnection или переменную DB_CONNECTION.");
}

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseLazyLoadingProxies());

builder.Services.AddScoped<CardService>();
builder.Services.AddTransient<BranchAndBoundOptimizer>();
builder.Services.AddTransient<BruteForceOptimizer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

static string? FirstNonEmpty(params string?[] values)
{
    return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

static string? ReadConnectionStringFromEnvFiles(string contentRootPath)
{
    var candidatePaths = new[]
    {
        Path.Combine(contentRootPath, ".env"),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "DeckOptimizer.UI", ".env"))
    };

    foreach (var path in candidatePaths)
    {
        if (!File.Exists(path))
        {
            continue;
        }

        var line = File.ReadLines(path)
            .FirstOrDefault(value => value.StartsWith("DB_CONNECTION=", StringComparison.OrdinalIgnoreCase));

        if (line == null)
        {
            continue;
        }

        return line["DB_CONNECTION=".Length..].Trim().Trim('"');
    }

    return null;
}
