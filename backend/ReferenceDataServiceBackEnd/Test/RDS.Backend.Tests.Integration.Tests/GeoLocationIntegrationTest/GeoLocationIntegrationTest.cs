using FluentAssertions;
using System.Net;
using Xunit.Abstractions;


namespace RDS.Backend.Tests.IntegrationTests
{
    [Collection("Geolocation Integration Tests")]
    [TestCaseOrderer("RDS.Backend.Tests.IntegrationTests.PriorityOrderer", "RDS.Backend.Tests.IntegrationTests")]
    public class GeolocationIntegrationTest(
        GeolocationIntegrationTestSupportFixture fixture)
        : IClassFixture<GeolocationIntegrationTestSupportFixture>
    {


        [Fact, TestPriority(1)]
        public async Task TestGetReferenceData()
        {
            var correlationId = Guid.NewGuid().ToString();
            var response = await fixture.SendRequestWithCorrelation(HttpMethod.Get, "/v1/reference-data", correlationId);
            response.Should().NotBeNull("Response should not be null.");
            response.StatusCode.Should().Be(HttpStatusCode.OK, "Response status code should be OK.");
            var correlationIdFromHeader = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            correlationId.Should().Be(correlationIdFromHeader, "Correlation ID should be the same.");
            var jsonResponse =
                await fixture.GetJsonResponse(response);
            jsonResponse.AsArray()?.Count.Should().BeGreaterThan(1000, "No reference data found in response.");
        }

        [Fact, TestPriority(3)]
        public async Task TestGetLocalities()
        {
            await fixture.SendBadGetRequest("/v1/localities");

            var response = await fixture.SendRequest(HttpMethod.Get, "/v1/localities");
            response.Should().NotBeNull("Response should not be null.");
            response.StatusCode.Should().Be(HttpStatusCode.OK, "Response status code should be OK.");
            var jsonResponse = await fixture.GetJsonResponse(response);
            jsonResponse.AsArray()?.Count.Should().BeGreaterThan(0, "No localities found in response.");
    
        }




        [Theory, TestPriority(4)]
        [InlineData("70")] // אשדוד
        [InlineData("3000")] // ירושלים
        public async Task TestGetLocalityById(string localityId)
        {
           await fixture.SendBadGetRequest($"/v1/localities/{localityId}");

            var response = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}");
            var data = await fixture.GetJsonResponse(response);

            data.Should().NotBeNull();

            var expectLocalityId = data["localityId"]?.ToString();
            expectLocalityId.Should().NotBeNullOrEmpty();

            expectLocalityId.Should().Be(localityId);
        }


        [Theory, TestPriority(5)]
        [InlineData("-1")] // not exist
        [InlineData("999999")] // not exist
        public async Task TestNotFoundGetLocalityById(string localityId)
        {
            await fixture.SendBadGetRequest($"/v1/localities/{localityId}");

            var response = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, "locality has been found");

            var jsonResponse = await fixture.GetJsonResponse(response);
            jsonResponse.GetValue<string>().Should().Be($"Locality with ID {localityId} not found.");
        }


        [Theory, TestPriority(9)]
        [InlineData("70")] // אשדוד
        [InlineData("3000")] // ירושלים
        public async Task TestGetStreetsByLocalityId(string localityId)
        {
            await fixture.SendBadGetRequest($"/v1/localities/{localityId}/streets");

            var response = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}/streets");
            var data = await fixture.GetJsonResponse(response);

            data.Should().NotBeNull();

            // Ensure "data" is an array and extract the first item
            var streets = data.AsArray();
            streets.Should().NotBeNull();
            streets.Count.Should().BeGreaterThan(0, "No streets found for the locality.");
            //validte is the same localityId in all streets
            foreach (var street in streets)
            {
                street["localityId"]?.ToString().Should().Be(localityId);
            }
        }


        [Theory, TestPriority(10)]
        [InlineData("-1")]
        [InlineData("999999")]
        public async Task TestGetStreetsByLocalityIdNotFound(string localityId)
        {
            await fixture.SendBadGetRequest($"/v1/localities/{localityId}/streets");

            var response = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}/streets");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"streets found for locality {localityId}.");

            var jsonResponse = await fixture.GetJsonResponse(response);
            jsonResponse.GetValue<string>().Should().Be($"No streets found for locality {localityId}.");
        }


        [Fact, TestPriority(2)]
        public async Task TestPostLocalities()
        {
            var correlationId = Guid.NewGuid().ToString();
            var response =  await fixture.UpsertLocalitiesAsync(correlationId);
            var coralationIdFromHeader = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            correlationId.Should().Be(coralationIdFromHeader, "Correlation ID should be the same.");


        }


        [Fact, TestPriority(8)]
        public async Task TestPostStreets()
        {
            var correlationId = Guid.NewGuid().ToString();
            var response = await fixture.UpsertStreetsAsync(correlationId);
            var coralationIdFromHeader = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            correlationId.Should().Be(coralationIdFromHeader, "Correlation ID should be the same.");
        }

        /// <summary>
        /// Test the cache invalidation process:
        /// 1. Upsert localities (populate DB).
        /// 2. Fetch localities (should come from DB).
        /// 3. Fetch localities again (should be cached).
        /// 4. Upsert localities again (invalidate cache).
        /// 5. Fetch localities (should come from DB again).
        /// </summary>
        [Fact, TestPriority(6)]
        public async Task TestCacheLocalities()
        {
            // Step 1: Upsert localities (populate DB)
            var upsertResponse1 = await fixture.UpsertLocalitiesAsync(Guid.NewGuid().ToString());
            upsertResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: Fetch localities (should come from DB)
            var fetchResponse1 = await fixture.SendRequest(HttpMethod.Get, "/v1/localities");
            var etag1 = fetchResponse1.Headers.GetValues("eTag").FirstOrDefault();


            // Step 3: Fetch localities again (should be cached)
            var fetchResponse2 = await fixture.SendRequest(HttpMethod.Get, "/v1/localities");
            var etag2 =  fetchResponse2.Headers.GetValues("eTag").FirstOrDefault();


            etag1.Should().Be(etag2, "Response should be cached.");

            // Step 4: Upsert localities again (invalidate cache)
            var upsertResponse2 = await fixture.UpsertLocalitiesAsync(Guid.NewGuid().ToString());
            upsertResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 5: Fetch localities (should come from DB again)
            var fetchResponse3 = await fixture.SendRequest(HttpMethod.Get, "/v1/localities");
            var etag3 = fetchResponse3.Headers.GetValues("eTag").FirstOrDefault();

            // Assert that cache invalidation worked (response is now different)
            etag3.Should().NotBe(etag1, "Response should not be cached after cache invalidation.");
        }


        [Fact, TestPriority(7)]
        public async Task TestGetLocalitieByIdCache()
        {
            var localityId = "3000"; 

            // Upsert localities (initial DB population)
            var upsertResponse1 = await fixture.UpsertLocalitiesAsync(Guid.NewGuid().ToString());
            upsertResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

            // Fetch Locality by ID (First Time - Should hit DB)
            var getResponse1 = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}");
            var etag1 = getResponse1.Headers.GetValues("eTag").FirstOrDefault();

            // Fetch Locality by ID Again (Should be from Cache)
            var getResponse2 = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}");
            var etag2 = getResponse2.Headers.GetValues("eTag").FirstOrDefault();

            // Assert caching
            etag1.Should().BeEquivalentTo(etag2);


            // Upsert localities again to invalidate cache
            await fixture.UpsertLocalitiesAsync(Guid.NewGuid().ToString());
            upsertResponse1.StatusCode.Should().Be(HttpStatusCode.OK);


            // Fetch Locality by ID Again (Should hit DB again, not cache)
            var getResponse3 = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}");
            var etag3 = getResponse3.Headers.GetValues("eTag").FirstOrDefault();
            etag3.Should().NotBeEquivalentTo(etag1);
        }

        [Fact, TestPriority(11)]
        public async Task TestGetStreetByIdCache()
        {
            var localityId = "472"; 

            // Upsert Streets (initial DB population)
            var upsertResponse1 = await fixture.UpsertStreetsAsync(Guid.NewGuid().ToString());
            upsertResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

            // Fetch Streets by localityId (First Time - Should hit DB)
            var getResponse1 = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}/streets");
            var etag1 = getResponse1.Headers.GetValues("eTag").FirstOrDefault();

            // Fetch Streets by localityId Again (Should be from Cache)
            var getResponse2 = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}/streets");
            var etag2 = getResponse2.Headers.GetValues("eTag").FirstOrDefault();

            // Assert caching
            etag1.Should().BeEquivalentTo(etag2);


            // Upsert localities again to invalidate cache
            await fixture.UpsertStreetsAsync(Guid.NewGuid().ToString());
            upsertResponse1.StatusCode.Should().Be(HttpStatusCode.OK);


            // Fetch Streets by localityId Again  (Should hit DB again, not cache)
            var getResponse3 = await fixture.SendRequest(HttpMethod.Get, $"/v1/localities/{localityId}/streets");
             var etag3 = getResponse3.Headers.GetValues("eTag").FirstOrDefault();
            etag3.Should().NotBeEquivalentTo(etag1);
        }


        [Fact, TestPriority(12)]
        public async Task TestNotSameCorrelationIDForStreet() 
        {
            string localityId = "70";
            // Fetch Streets by localityId (First Time - Should hit DB)
            var getResponse1 = await fixture.SendRequestWithCorrelation(HttpMethod.Get, $"/v1/localities/{localityId}/streets", Guid.NewGuid().ToString());
            var correlationId = getResponse1.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            var etag1 = getResponse1.Headers.GetValues("eTag").FirstOrDefault();
       
            // Fetch Streets by localityId Again (Should be from Cache)
            var getResponse2 = await fixture.SendRequestWithCorrelation(HttpMethod.Get, $"/v1/localities/{localityId}/streets", Guid.NewGuid().ToString());
            var correlationIdFromHeader2 = getResponse2.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            var etag2 = getResponse2.Headers.GetValues("eTag").FirstOrDefault();
          

            // Assert not the same 
            correlationId.Should().NotBe(correlationIdFromHeader2, "Response should not be cached.");

            // Assert the same eTag
            etag1.Should().Be(etag2, "Response should be cached.");
        }

        [Theory, TestPriority(13)]
        [InlineData("")]
        [InlineData("/154")]
        public async Task TestNotSameCorrelationIDForlocality(string localityId)
        {

            // Fetch Streets by localityId (First Time - Should hit DB)
            var getResponse1 = await fixture.SendRequestWithCorrelation(HttpMethod.Get, $"/v1/localities{localityId}", Guid.NewGuid().ToString());
            var correlationId = getResponse1.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            var etag1 = getResponse1.Headers.GetValues("eTag").FirstOrDefault();

            // Fetch Streets by localityId Again (Should be from Cache)
            var getResponse2 = await fixture.SendRequestWithCorrelation(HttpMethod.Get, $"/v1/localities{localityId}", Guid.NewGuid().ToString());
            var correlationIdFromHeader2 = getResponse2.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
            var etag2 = getResponse2.Headers.GetValues("eTag").FirstOrDefault();


            // Assert not the same correlationId
            correlationId.Should().NotBe(correlationIdFromHeader2, "Response should not be cached.");

            // Assert the same eTag
            etag1.Should().Be(etag2, "Response should be cached.");

        }



    }
}