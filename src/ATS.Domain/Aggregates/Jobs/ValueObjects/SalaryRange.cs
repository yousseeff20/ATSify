using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Jobs.ValueObjects;

public sealed class SalaryRange : ValueObject
{
    public decimal Min { get; private set; }
    public decimal Max { get; private set; }
    public string Currency { get; private set; }

    private SalaryRange() { Currency = null!; } // EF Core

    public SalaryRange(decimal min, decimal max, string currency)
    {
        if (min < 0)
            throw new ArgumentException("Min salary cannot be negative.", nameof(min));
        if (max < min)
            throw new ArgumentException("Max salary cannot be less than min salary.", nameof(max));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        Min = min;
        Max = max;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Min;
        yield return Max;
        yield return Currency;
    }
}
