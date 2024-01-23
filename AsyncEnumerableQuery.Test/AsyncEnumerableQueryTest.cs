using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace AsyncEnumerableQuery.Test;

public class AsyncEnumerableQueryTest {
    private readonly ITestOutputHelper _testOutput;

    public AsyncEnumerableQueryTest(ITestOutputHelper testOutput) {
        _testOutput = testOutput;
    }
    
    [Fact]
    public async Task TestToListAsync() {
        var queryable = new[] {
            new WeatherForecastEntity {
                Id = new Guid("0224589c-bc34-4761-b8e6-8b95aba85a00"),
                Date = new DateOnly(2010, 01, 01),
                TemperatureC = 12,
                CreatedAt = new DateTimeOffset(2023, 10, 23, 0, 0, 0, TimeSpan.Zero)
            }
        }.AsAsyncQueryable();
        var forecasts = await queryable
            .Where(x => x.TemperatureC <= 12)
            .ToListAsync();
        _testOutput.WriteLine(string.Join(',', forecasts));
        forecasts.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task TestWhereWithToListAsync() {
        var queryable = new[] {
            new WeatherForecastEntity {
                Id = new Guid("0224589c-bc34-4761-b8e6-8b95aba85a00"),
                Date = new DateOnly(2010, 01, 01),
                TemperatureC = 12,
                CreatedAt = new DateTimeOffset(2023, 10, 23, 0, 0, 0, TimeSpan.Zero)
            }
        }.AsAsyncQueryable();
        var forecasts = await queryable
            .Where(x => x.TemperatureC > 12)
            .ToListAsync();
        _testOutput.WriteLine(string.Join(',', forecasts));
        forecasts.Should().HaveCount(0);
    }
    
    [Fact]
    public async Task TestCountAsync() {
        var queryable = new[] {
            new WeatherForecastEntity {
                Id = new Guid("0224589c-bc34-4761-b8e6-8b95aba85a00"),
                Date = new DateOnly(2010, 01, 01),
                TemperatureC = 12,
                CreatedAt = new DateTimeOffset(2023, 10, 23, 0, 0, 0, TimeSpan.Zero)
            }
        }.AsAsyncQueryable();
        (await queryable.CountAsync()).Should().Be(1);
    }
    
    [Fact]
    public async Task TestSelectFirstOrDefaultAsync() {
        var queryable = new[] {
            new WeatherForecastEntity {
                Id = new Guid("0224589c-bc34-4761-b8e6-8b95aba85a00"),
                Date = new DateOnly(2010, 01, 01),
                TemperatureC = 12,
                CreatedAt = new DateTimeOffset(2023, 10, 23, 0, 0, 0, TimeSpan.Zero)
            }
        }.AsAsyncQueryable();
        (await queryable.Select(x => x.TemperatureC).FirstOrDefaultAsync()).Should().Be(12);
    }
}