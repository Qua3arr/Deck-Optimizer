namespace DeckOptimizer.WebAPI.Contracts
{
    public class CharacteristicDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CharacteristicValueDto
    {
        public Guid CharacteristicId { get; set; }
        public string CharacteristicName { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class CardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public List<CharacteristicValueDto> Characteristics { get; set; } = new();
    }

    public class CreateCardRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public Dictionary<Guid, double> CharacteristicValues { get; set; } = new();
    }

    public class UpdateCardRequest
    {
        public string? Name { get; set; }
        public decimal? Cost { get; set; }
        public Dictionary<Guid, double>? CharacteristicValues { get; set; }
    }

    public class CreateCharacteristicRequest
    {
        public string Name { get; set; } = string.Empty;
        public double DefaultValue { get; set; }
    }

    public class OptimizeDeckRequest
    {
        public decimal MaxCost { get; set; }
        public int DeckSize { get; set; }
        public Dictionary<Guid, double> Weights { get; set; } = new();
    }

    public class CharacteristicAggregateDto
    {
        public Guid CharacteristicId { get; set; }
        public string CharacteristicName { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class OptimizationResponse
    {
        public bool HasSolution { get; set; }
        public decimal TotalCost { get; set; }
        public double AggregatedValue { get; set; }
        public double CalculationTimeMs { get; set; }
        public int VisitedNodeCount { get; set; }
        public List<CardDto> SelectedCards { get; set; } = new();
        public List<CharacteristicAggregateDto> AggregatedCharacteristics { get; set; } = new();
    }

    public class ExperimentResultDto
    {
        public int CardCount { get; set; }
        public int DeckSize { get; set; }
        public decimal MaxCost { get; set; }
        public bool HasSolution { get; set; }
        public double AggregatedValue { get; set; }
        public double BranchAndBoundTimeMs { get; set; }
        public int BranchAndBoundVisitedNodes { get; set; }
        public long CombinationCount { get; set; }
        public bool BruteForceWasRun { get; set; }
        public double? BruteForceTimeMs { get; set; }
        public bool? MatchesBruteForce { get; set; }
    }
}
