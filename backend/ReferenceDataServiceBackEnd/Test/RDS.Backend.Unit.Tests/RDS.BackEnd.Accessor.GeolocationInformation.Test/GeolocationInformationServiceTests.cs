using System.Net;
using RDS.BackEnd.Accessor.GeolocationInformation.Exceptions;
namespace RDS.BackEnd.Accessor.GeolocationInformation.Test;
using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Models;
using Services;

public class GeolocationInformationServiceTests(GeolocationInformationServiceFixture fixture)
    : IClassFixture<GeolocationInformationServiceFixture>
{
    [Fact]
    public async Task GetLocalitiesAsync_ReturnsLocalities()
    {
        var expected = new List<LocalityDto>
        {
            new() { LocalityId = "001", LocalityName = "Tel Aviv" },
            new() { LocalityId = "002", LocalityName = "Jerusalem" }
        };

        fixture.MockDatabaseService.Setup(x => x.GetLocalitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await fixture.GeolocationService.GetLocalitiesAsync(CancellationToken.None);

        result.Should().HaveCount(expected.Count);
        result[0].LocalityName.Should().Be("Tel Aviv");
        result[1].LocalityName.Should().Be("Jerusalem");
    }

    [Fact]
    public async Task GetLocalityByIdAsync_ReturnsCorrectLocality()
    {
        var expected = new LocalityDto { LocalityId = "123", LocalityName = "Haifa" };

        fixture.MockDatabaseService.Setup(x => x.GetLocalityByIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await fixture.GeolocationService.GetLocalityByIdAsync("123", CancellationToken.None);

        result.Should().NotBeNull();
        result.LocalityName.Should().Be("Haifa");
    }

    [Fact]
    public async Task UpsertLocalitiesAsync_CallsDbAndEvictsCache()
    {
        var localities = new List<Locality> { new() { localityId = "101", localityName = "Eilat" } };

        await fixture.GeolocationService.UpsertLocalitiesAsync(localities, CancellationToken.None);

        fixture.MockDatabaseService.Verify(x =>
                x.UpsertLocalitiesAsync(localities, It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.MockCacheService.Verify(x => x.EvictCacheAsync("localities", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertStreetDataAsync_WithNewTimestamp_EvictsCache()
    {
        var request = new StreetBatchRequest
        {
            Streets = new List<Street> { new() { streetId = "1", streetName = "Herzl" } },
            UpdateTimestamp = DateTime.UtcNow.AddMinutes(1)
        };

        await fixture.GeolocationService.UpsertStreetDataAsync(request, CancellationToken.None);

        fixture.MockDatabaseService.Verify(x => x.UpsertStreetDataAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        fixture.MockCacheService.Verify(x => x.EvictCacheAsync("streets", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertStreetDataAsync_WithOldTimestamp_DoesNotEvictCache()
    {
        var initialRequest = new StreetBatchRequest
        {
            Streets = [new Street { streetId = "init", streetName = "Init St" }],
            UpdateTimestamp = DateTime.UtcNow
        };
        await fixture.GeolocationService.UpsertStreetDataAsync(initialRequest, CancellationToken.None);

        var oldRequest = new StreetBatchRequest
        {
            Streets = [new Street { streetId = "old", streetName = "Old St" }],
            UpdateTimestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        await fixture.GeolocationService.UpsertStreetDataAsync(oldRequest, CancellationToken.None);

        fixture.MockCacheService.Verify(x => x.EvictCacheAsync("streets", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLocalitiesAsync_ThrowsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        fixture.MockDatabaseService
            .Setup(x => x.GetLocalitiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Func<Task> act = async () => await fixture.GeolocationService.GetLocalitiesAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetLocalitiesAsync_CancelsDuringDelay()
    {
        fixture.MockDatabaseService
            .Setup(x => x.GetLocalitiesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async token =>
            {
                await Task.Delay(1000, token);
                return new List<LocalityDto>();
            });

        using var cts = new CancellationTokenSource(100);

        Func<Task> act = async () => await fixture.GeolocationService.GetLocalitiesAsync(cts.Token);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task GetLocalitiesAsync_PassesCancellationTokenToDatabaseService()
    {
        var cts = new CancellationTokenSource();

        fixture.MockDatabaseService
            .Setup(x => x.GetLocalitiesAsync(It.Is<CancellationToken>(t => t == cts.Token)))
            .ReturnsAsync([]);

        var result = await fixture.GeolocationService.GetLocalitiesAsync(cts.Token);

        result.Should().BeEmpty();
        fixture.MockDatabaseService.Verify(x => x.GetLocalitiesAsync(cts.Token), Times.Once);
        cts.Dispose();
    }

    [Fact]
    public async Task CosmosDbService_GetLocalitiesAsync_ThrowsWhenCancelledDuringRead()
    {
        var mockIterator = new Mock<FeedIterator<LocalityDto>>();
        mockIterator.Setup(x => x.HasMoreResults).Returns(true);
        mockIterator
            .Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var mockContainer = new Mock<Container>();
        mockContainer
            .Setup(x => x.GetItemQueryIterator<LocalityDto>(It.IsAny<QueryDefinition>(), null, null))
            .Returns(mockIterator.Object);

        typeof(CosmosDbService)
            .GetField("_localitiesContainer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(fixture.CosmosDbService, mockContainer.Object);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = async () => await fixture.CosmosDbService.GetLocalitiesAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task GetLocalitiesAsync_WhenCosmosReturns429_ThrowsCosmosException()
    {
      
        var cosmosException = new CosmosException(
            message: "Too many requests",
            statusCode: HttpStatusCode.TooManyRequests,
            subStatusCode: 0,
            activityId: Guid.NewGuid().ToString(),
            requestCharge: 0);

        var mockIterator = new Mock<FeedIterator<LocalityDto>>();
        mockIterator.Setup(i => i.HasMoreResults).Returns(true);
        mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(cosmosException);

        var mockContainer = new Mock<Container>();
        mockContainer.Setup(c =>
                c.GetItemQueryIterator<LocalityDto>(It.IsAny<QueryDefinition>(), null, null))
            .Returns(mockIterator.Object);

        // Inject the mocked container into the CosmosDbService (private field)
        typeof(CosmosDbService)
            .GetField("_localitiesContainer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(fixture.CosmosDbService, mockContainer.Object);

      
        Func<Task> act = async () => await fixture.CosmosDbService.GetLocalitiesAsync(CancellationToken.None);

       
        var ex = await act.Should().ThrowAsync<CosmosException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GetLocalitiesAsync_ThrowsException_WhenServiceFails()
    {
       
        fixture.MockDatabaseService
            .Setup(x => x.GetLocalitiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Internal server error"));
    
    
        Func<Task> act = async () => await fixture.GeolocationService.GetLocalitiesAsync(CancellationToken.None);
        
        var ex = await act.Should().ThrowAsync<Exception>();
        ex.Which.Message.Should().Be("Internal server error");
    }

    [Fact]
    public async Task UpsertStreetDataAsync_ThrowsBatchUpsertFailedException_WhenUpsertFails()
    {
  
        var request = new StreetBatchRequest
        {
            Streets =
            [
                new Street { streetId = "failedStreet1", streetName = "Failed Street 1" },
                new Street { streetId = "failedStreet2", streetName = "Failed Street 2" }
            ],
            UpdateTimestamp = DateTime.UtcNow
        };
    
        fixture.MockDatabaseService
            .Setup(x => x.UpsertStreetDataAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BulkUpsertFailedException("Street",new List<string> { "failedStreet1", "failedStreet2" }));
    
     
        Func<Task> act = async () => await fixture.GeolocationService.UpsertStreetDataAsync(request, CancellationToken.None);
    
     
        var ex = await act.Should().ThrowAsync<BulkUpsertFailedException>();
        ex.Which.FailedIds.Should().BeEquivalentTo(["failedStreet1", "failedStreet2"]);
    }
}
