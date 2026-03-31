namespace DeckOptimizer.Domain.Entities
{
    public class CharacteristicValue
    {
        public Guid CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        public Guid CharacteristicId { get; set; }
        public virtual Characteristic Characteristic { get; set; } = null!;

        //Значение характеристики для этой карты (например, Сила = 10.5)
        public double Value { get; set; }
    }
}
