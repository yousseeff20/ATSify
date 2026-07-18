using ATS.Domain.Aggregates.Jobs.ValueObjects;
using FluentAssertions;

namespace ATS.Domain.UnitTests.Aggregates.Jobs;

public class SalaryRangeTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateSalaryRange()
    {
        // Act
        var range = new SalaryRange(50000, 100000, "USD");

        // Assert
        range.Min.Should().Be(50000);
        range.Max.Should().Be(100000);
        range.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WhenMinIsNegative_ShouldThrowException()
    {
        // Act
        Action action = () => new SalaryRange(-1, 100000, "USD");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Min salary cannot be negative. (Parameter 'min')");
    }

    [Fact]
    public void Constructor_WhenMaxIsLessThanMin_ShouldThrowException()
    {
        // Act
        Action action = () => new SalaryRange(100000, 50000, "USD");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Max salary cannot be less than min salary. (Parameter 'max')");
    }
}
