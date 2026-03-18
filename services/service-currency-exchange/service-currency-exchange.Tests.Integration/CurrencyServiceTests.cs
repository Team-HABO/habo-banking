using FluentAssertions;

namespace service_currency_exchange.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="service_currency_exchange.Services.CurrencyService"/>
/// that make real HTTP calls to the Frankfurter public API (https://api.frankfurter.dev).
/// Requires outbound internet access.
/// </summary>
[Trait("Category", "Integration")]
public class CurrencyServiceTests(CurrencyServiceFixture fixture) : IClassFixture<CurrencyServiceFixture>
{
    [Fact]
    public async Task GetRateFromDkkAsync_WhenCurrencyIsSupported_ReturnsPositiveRate()
    {
        var rate = await fixture.CurrencyService.GetRateFromDkkAsync("USD");

        rate.Should().NotBeNull();
        rate.GetValueOrDefault().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRateFromDkkAsync_WhenCurrencyIsUnknown_ReturnsNull()
    {
        var rate = await fixture.CurrencyService.GetRateFromDkkAsync("XYZ");

        rate.Should().BeNull("Frankfurter does not know the currency 'XYZ'");
    }

    [Fact]
    public async Task GetRateFromDkkAsync_CurrencyCodeIsCaseInsensitive()
    {
        var upper = await fixture.CurrencyService.GetRateFromDkkAsync("EUR");
        var lower = await fixture.CurrencyService.GetRateFromDkkAsync("eur");

        upper.Should().NotBeNull();
        lower.Should().NotBeNull();
        lower.GetValueOrDefault().Should().BeApproximately(upper.GetValueOrDefault(), 0.0001m);
    }
}


