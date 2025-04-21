using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using System.Text.Json.Nodes;
using System.Net;

namespace RDS.Backend.Tests.IntegrationTests
{
    public class GeolocationIntegrationTestSupportFixture : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _localHttpClient;


        public GeolocationIntegrationTestSupportFixture()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            _localHttpClient = _httpClientFactory.CreateClient("GeolocationTestClient");
        }

        public HttpClient HttpClient => _localHttpClient;


        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            string baseUrl = configuration["GeolocationApi:BaseUrl"] ?? "http://localhost:64402";

            services.AddHttpClient("GeolocationTestClient", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("User-Agent", "RDS Geolocation Test Client");
            });
        }


        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120)); // Ensure a 120-second timeout

            return await _localHttpClient.SendAsync(request, cts.Token);
        }

        public async Task<HttpResponseMessage> SendRequestWithCorrelation(HttpMethod method, string url, string Correlation)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Correlation-ID", Correlation);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120)); // Ensure a 120-second timeout

            return await _localHttpClient.SendAsync(request, cts.Token);
        }

        public async Task SendBadGetRequest(string url)
        {
            var badResponse = await _localHttpClient.GetAsync(url);
            badResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "No correlation given");
            var badResponseJson = await GetJsonResponse(badResponse);
            badResponseJson.Should().NotBeNull();
            var error = badResponseJson["error"]?.ToString();
            error.Should().Be("Invalid or missing Correlation ID");
        }


        public async Task<JsonNode> GetJsonResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty("Response content should not be empty.");
            var jsonResponse = JsonNode.Parse(content, new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            jsonResponse.Should().NotBeNull("Parsed JSON response should not be null.");
            return jsonResponse!;
        }

        public async Task<HttpResponseMessage> UpsertLocalitiesAsync(string correlationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/localities");
            request.Headers.Add("X-Correlation-ID", correlationId);
            request.Content = new StringContent(string.Empty);

            var response = await _localHttpClient.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        public async Task<HttpResponseMessage> UpsertStreetsAsync(string correlationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/streets");
            request.Headers.Add("X-Correlation-ID", correlationId);
            request.Content = new StringContent(string.Empty);

            var response = await _localHttpClient.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        public void Dispose()
        {
            _localHttpClient.Dispose();
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}