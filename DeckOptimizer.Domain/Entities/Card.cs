namespace DeckOptimizer.Domain.Entities
{
    public class Card
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }

        //Навигационное свойство для связи с характеристиками
        public virtual ICollection<CharacteristicValue> CharacteristicValues { get; set; } = new List<CharacteristicValue>();
    }
}
