# reference-data-service

# Reference Data Service - Backend

## Overview

The Reference Data Service (RDS) Backend is a microservices-based architecture designed to manage and update geolocation reference data, including localities and streets. The system utilizes **Dapr**, **CosmosDB**, and **Docker Compose** to ensure efficient data retrieval, updating, and scheduling.

This documentation provides an in-depth explanation of the project, its architecture, technologies used, API endpoints, telemetry, observability, health checks, and deployment instructions.

---

## 📌 Technologies Used

| Technology  | Purpose |
|-------------|---------|
| **ASP.NET Core Minimal APIs** | Provides a lightweight REST API for geolocation data |
| **Dapr** | Facilitates inter-service communication and state management |
| **Docker Compose** | Manages services and dependencies in containerized environments |
| **CosmosDB** | Stores geolocation data persistently |
| **OpenTelemetry** | Enables telemetry and observability |
| **Health Checks API** | Monitors system health and service availability |
| **FluentAssertions & xUnit** | Implements unit and integration testing |

---

## 🏗 Project Architecture

The project consists of multiple microservices running as independent containers, communicating through **Dapr sidecars**. The key services include:

- **GeoLocation Manager Service** (`rds.backend.manager.geolocation`)  
  Handles API endpoints and triggers updates to the database.
- **Government GeoLocation Provider Accessor Service** (`rds.backend.accessor.governmentgeolocationprovider`)  
  Fetches geolocation data from external government sources.
- **GeoLocation Information Accessor Service** (`rds.backend.accessor.geolocationinformation`)  
  Stores and retrieves geolocation data in **CosmosDB**.
- **Zipkin**  
  Zipkin enables distributed tracing.

---

## 🛠 API Endpoints

### 🌍 **Reference Data Endpoints**

#### ✅ Get Reference Data
```
GET /v1/reference-data
```
- **Response:** JSON containing government-provided geolocation data.

#### ✅ Get All Localities
```
GET /v1/localities
```
- **Headers:** `X-Correlation-ID: <UUID>` (Required)
- **Response:** List of available localities.

#### ✅ Get Locality by ID
```
GET /v1/localities/{id}
```
- **Headers:** `X-Correlation-ID: <UUID>` (Required)
- **Response:** Details of the specified locality.

#### ✅ Get Streets in a Locality
```
GET /v1/localities/{id}/streets
```
- **Headers:** `X-Correlation-ID: <UUID>` (Required)
- **Response:** List of streets in the locality.

---

### 📌 **Update Endpoints**

#### ✅ Update Localities
```
POST /v1/localities
```
- **Request Body:** None (fetches latest data automatically)
- **Response:** Status of update.

#### ✅ Update Streets
```
POST /v1/streets
```
- **Request Body:** None (fetches latest data automatically)
- **Response:** Status of update.

---

## 🔄 Dynamic Configuration with IOptionsMonitor

### The system supports runtime configuration updates for the GovernmentApiOptions using IOptionsMonitor<T>. This enables you to update values like the base URL or endpoint paths in appsettings.json without restarting the service.

```json
  "GovernmentApi": {
    "BaseUrl": "https://data.gov.il/api/3/action/",
    "LocalitiesEndpoint": "datastore_search?resource_id=5c78e9fa-c2e2-4771-93ff-7f400a12f7ba&limit=1000000",
    "StreetsEndpoint": "datastore_search?resource_id=9ad3862c-8391-4b2f-84a4-2d4c68625f4b&limit=1000000"
  }
```

---

## ⚙️ Internal Configuration

### The project uses a centralized InternalConfiguration.cs file in each service to store all magic numbers, timeouts, and default constants for better maintainability and environment-independent behavior.

Example (GeolocationInformation Accessor):
```csharp
public static class InternalConfiguration
{
    public static readonly List<KeyValuePair<string, string>> Default = new()
    {
        new("MaxUpsertsInParallel", "4"),
        new("RetryCount", "3"),
        new("JitterMaxMilliseconds", "100"),
        new("PartitionKeyPath", "/localityId")
    };

    public static readonly List<KeyValuePair<string, TimeSpan>> Timeouts = new()
    {
        new("OutputCacheExpiration", TimeSpan.FromMinutes(2))
    };
}

```

---

## 🔍 Health Checks

The system includes built-in health checks for monitoring service availability.

### ✅ **Endpoints**

- **Liveness Probe:** `GET /liveness` – Ensures the service is running.
- **Readiness Probe:** `GET /readiness` – Ensures all dependencies are ready.
- **Full Health Check:** `GET /health` – Provides detailed health status.

### ✅ **Monitored Components**
- **Self-check** – Confirms the API is reachable.
- **CosmosDB** – Validates database connectivity.
---

## 📊 Telemetry & Observability

The project integrates **OpenTelemetry** for tracing, logging, and metrics collection.

### ✅ **Collected Data**
- **ASP.NET Core Instrumentation** – Tracks incoming requests.
- **HttpClient Instrumentation** – Monitors outgoing API calls.
- **Runtime Metrics** – Provides CPU & memory usage statistics.
- **Zipkin Tracing** – Enables distributed tracing.

### ✅ **Configuration in `Program.cs`**
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GeoLocationManager"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GeoLocationManager"))
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();
    });
```

---

## 🐳 Running the Project

### 📍 **Navigate to the 'ReferenceDataServiceBackEnd' folder**

```
cd reference-data-service\backend\ReferenceDataServiceBackEnd
```

### 🔥 **Start the Entire Stack**
Ensure **Docker** is installed and run:

```
docker-compose up --build
```

This will start all services including Redis, Zipkin, and CosmosDB.

### 🔍 **Testing APIs**
Use **Postman** or **cURL** to test endpoints.

```bash
curl -X GET "http://localhost:64402/v1/localities" -H "X-Correlation-ID: $(uuidgen)"
```

## ✅ Run Tests
The project uses **xUnit** and **FluentAssertions** for integration testing.

### 📍 **Navigate to the 'GeoLocationIntegrationTest' folder**  

```
cd reference-data-service\backend\ReferenceDataServiceBackEnd\Test\RDS.Backend.Tests.Integration.Tests\GeoLocationIntegrationTest
```
### 🔥 **Run** 
```
dotnet test
```
This will start all the tests and display the output in the terminal.


### 🔥 **Second option is to Start the Test with Test Explorer**

Start RDS.Backend.Tests.IntegrationTests.sln and enter 'Test Explorer'  
This will start the test explorer and display all the available tests.




## 📦 Semantic Versioning (SemVer)

This project uses Semantic Versioning powered by GitVersion to manage consistent, automatic, and traceable version numbers across all microservices.

### 🔧 How It Works
During the CI build process:

* A version is automatically calculated by GitVersion using the commit history and branch name.

* Three build arguments are injected into each Docker image:

  * VERSION – The full semantic version (e.g. 1.2.3, or 0.1.5-featureX.4)

  * GIT_COMMIT – The short SHA of the latest Git commit

  * BUILD_TIME – The UTC timestamp of the image build

These are set as environment variables inside the container and exposed via a dedicated endpoint (/version).

### 🧪 When Is the Version Calculated?
The GitHub Actions CI workflow runs in the following scenarios:

1. On Pull Request to main – Automatically calculates and builds versioned images.

2. On Tag Push – Triggered when pushing a Git tag starting with v (e.g. v1.0.0).

3. Manual Execution – The workflow can also be triggered manually via the "Run workflow" button in the GitHub Actions tab.

To push a release tag manually:
```bash
git tag v1.0.0
git push origin v1.0.0
```