namespace AsyncEnumerableQuery.Test;

public sealed class WeatherForecastEntity {
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}