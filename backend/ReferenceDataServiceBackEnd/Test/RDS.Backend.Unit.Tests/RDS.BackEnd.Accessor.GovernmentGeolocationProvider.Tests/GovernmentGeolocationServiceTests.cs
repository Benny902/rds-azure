using FluentAssertions;
using Moq;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Models;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Tests;

public class GovernmentGeolocationServiceTests(GovernmentGeolocationServiceFixture fixture)
    : IClassFixture<GovernmentGeolocationServiceFixture>
{
    [Fact]
    public async Task TestFetchGeolocationDataAsync()
    {
        var rawData = "[{\"localityId\": \"123\", \"localityName\": \"Tel Aviv\"}]";
        var expectedLocalities = new List<Locality>
        {
            new() { localityId = "123", localityName = "Tel Aviv" }
        };

        fixture.MockApiClient.Setup(client =>
                client.GetRawDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawData);

        fixture.MockParser.Setup(parser => parser.ParseLocalities(rawData))
            .Returns(expectedLocalities);

        var result = await fixture.ServiceUnderTest.FetchGeolocationDataAsync(CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result[0].localityName.Should().Be("Tel Aviv");
    }

    [Fact]
    public async Task TestFetchGeolocationDataReturnEmptyList()
    {
        fixture.MockApiClient.Setup(client =>
                client.GetRawDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var result = await fixture.ServiceUnderTest.FetchGeolocationDataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task TestFetchStreetDataAsync_ShouldReturnValidData()
    {
        var rawData = "[{\"localityId\": \"123\", \"streetId\": \"456\", \"streetName\": \"Herzl Street\"}]";
        var expectedStreets = new List<Street>
        {
            new() { localityId = "123", streetId = "456", streetName = "Herzl Street" }
        };

        fixture.MockApiClient.Setup(client =>
                client.GetRawDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawData);

        fixture.MockParser.Setup(parser => parser.ParseStreets(rawData))
            .Returns(expectedStreets);

        var result = await fixture.ServiceUnderTest.FetchStreetDataAsync(CancellationToken.None);

        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result[0].streetName.Should().Be("Herzl Street");
    }

    [Fact]
    public async Task TestFetchStreetDataAsync_ShouldReturnEmptyList()
    {
        fixture.MockApiClient.Setup(client =>
                client.GetRawDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var result = await fixture.ServiceUnderTest.FetchStreetDataAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}