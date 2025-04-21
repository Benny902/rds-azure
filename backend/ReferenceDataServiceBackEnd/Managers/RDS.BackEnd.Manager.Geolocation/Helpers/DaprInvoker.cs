using System.Globalization;
using System.Net;
using System.Text.Json;
using Dapr.Client;
using Polly;
using Grpc.Core;
using RDS.BackEnd.Manager.Geolocation.Exceptions;

namespace RDS.BackEnd.Manager.Geolocation.Helpers
{
    public class DaprInvoker(DaprClient daprClient, ILogger<DaprInvoker> logger)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<TResponse> InvokeAsync<TResponse>(HttpMethod method, string appId, string endpoint)
        {
            try
            {
                logger.LogDebug("Invoking {AppId} {Method} {Endpoint}", appId, method, endpoint);
                return await daprClient.InvokeMethodAsync<TResponse>(method, appId, endpoint);
            }
            catch (InvocationException ex) when (IsNotFound(ex))
            {
                logger.LogError(ex, "Dapr 404 Not Found for {AppId}/{Endpoint}", appId, endpoint);
                throw new KeyNotFoundException("Requested data was not found.");

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dapr invocation failed: {AppId}/{Endpoint}", appId, endpoint);
                throw new ApplicationException("Failed to invoke service through Dapr.");
            }
        }

        private async Task<TResponse> InvokeAsync<TRequest, TResponse>(HttpMethod method, string appId, string endpoint, TRequest body)
        {
            try
            {
                logger.LogDebug("Invoking {AppId} {Method} {Endpoint} with body", appId, method, endpoint);

                var request = daprClient.CreateInvokeMethodRequest(method, appId, endpoint);
                request.Content = JsonContent.Create(body);

                var response = await daprClient.InvokeMethodWithResponseAsync(request);

                // Throw if non-success status code (including 500, 400, etc.)
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogError("Dapr call failed with status {StatusCode}: {Content}", response.StatusCode, content);

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        logger.LogError("Cosmos DB returned a 400 Bad Request , and not upsert the all data.");
                        throw new UpsertFailedException();
                    }
                    
                    
                    
                    throw new ApplicationException($"Dapr call to {appId}{endpoint} failed: {response.StatusCode} - {content}");
                }

                if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                    return default!;

                return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions)
                    ?? throw new ApplicationException("Dapr returned empty or invalid JSON.");
            }
            
            catch (InvocationException ex) when (IsNotFound(ex))
            {
                logger.LogError(ex, "Dapr 404 Not Found for {AppId}/{Endpoint}", appId, endpoint);
                throw new KeyNotFoundException("Requested data was not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dapr invocation failed: {AppId}/{Endpoint}", appId, endpoint);
                throw new ApplicationException("Failed to invoke service through Dapr.");
            }
        }

        public HttpRequestMessage CreateRequest(HttpMethod method, string appId, string endpoint)
        {
            try
            {
                return daprClient.CreateInvokeMethodRequest(method, appId, endpoint);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create request for {AppId}/{Endpoint}", appId, endpoint);
                throw new ApplicationException("Failed to create request for Dapr invocation.");
            }
        }

        public async Task<HttpResponseMessage> InvokeWithResponseAsync(HttpRequestMessage request)
        {
            try
            {
                return await daprClient.InvokeMethodWithResponseAsync(request);
            }
            catch (InvocationException ex) when (IsNotFound(ex))
            {
                logger.LogError(ex, "Dapr 404 Not Found on request {Method} {Uri}", request.Method, request.RequestUri);
                throw new KeyNotFoundException("Requested data was not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dapr invocation failed on request {Method} {Uri}", request.Method, request.RequestUri);
                throw new ApplicationException("Failed to invoke service through Dapr.");
            }
        }

        public async Task<TResponse> InvokeGetAsync<TResponse>(string appId, string endpoint)
        {
            return await InvokeAsync<TResponse>(HttpMethod.Get, appId, endpoint);
        }

        public async Task InvokePostAsync<TRequest>(string appId, string endpoint, TRequest body)
        {
            await InvokeAsync<TRequest, object>(HttpMethod.Post, appId, endpoint, body);
        }


        private static bool IsNotFound(Exception ex)
        {
            return ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase)
                || ex.InnerException?.Message.Contains("404") == true
                || (ex.InnerException is RpcException rpc && rpc.StatusCode == StatusCode.NotFound);
        }

    }
}