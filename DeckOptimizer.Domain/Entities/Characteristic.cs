namespace DeckOptimizer.Domain.Entities
{
    public class Characteristic
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }
}
