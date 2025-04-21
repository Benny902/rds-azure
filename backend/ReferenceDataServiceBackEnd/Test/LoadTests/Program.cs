using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;

var httpClient = HttpClientFactory.Create();


var baseUrl = "http://localhost:64402/";


string CombineUrl(string baseUrl, string endpoint)
    => $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

// -------------------------------
// GET: /v1/localities
// -------------------------------
var getLocalitiesScenario = Scenario.Create("Get Localities", async _ =>
{
    var request = Http.CreateRequest("GET", CombineUrl(baseUrl, "v1/localities"))
                      .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString());

    var response = await Http.Send(httpClient, request);
    return response;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(Simulation.KeepConstant(20, TimeSpan.FromSeconds(30)));

// -------------------------------
// GET: /v1/localities/70/streets
// -------------------------------
var getStreetsScenario = Scenario.Create("Get Streets By LocalityId", async _ =>
{
    var request = Http.CreateRequest("GET", CombineUrl(baseUrl, "v1/localities/70/streets"))
                      .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString());

    var response = await Http.Send(httpClient, request);
    return response;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(Simulation.KeepConstant(20, TimeSpan.FromSeconds(30)));

// -------------------------------
// GET: /v1/localities/3000
// -------------------------------
var getLocalityByIdScenario = Scenario.Create("Get Locality By Id", async _ =>
{
    var request = Http.CreateRequest("GET", CombineUrl(baseUrl, "v1/localities/3000"))
                      .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString());

    var response = await Http.Send(httpClient, request);
    return response;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(Simulation.KeepConstant(20, TimeSpan.FromSeconds(30)));

// -------------------------------
// POST: /v1/localities
// -------------------------------
var postLocalitiesScenario = Scenario.Create("Post Localities", async _ =>
{
    var request = Http.CreateRequest("POST", CombineUrl(baseUrl, "v1/localities"))
                      .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString())
                      .WithBody(new StringContent("{}", Encoding.UTF8, "application/json")); // גוף ריק אבל תקין

    var response = await Http.Send(httpClient, request);
    return response;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(1))
.WithLoadSimulations(Simulation.KeepConstant(1, TimeSpan.FromMinutes(1))); // לאפשר זמן לביצוע מלא

// -------------------------------
// POST: /v1/streets
// -------------------------------
//var postStreetsScenario = Scenario.Create("Post Streets", async _ =>
//{
//    var request = Http.CreateRequest("POST", CombineUrl(baseUrl, "v1/streets"))
//                      .WithHeader("X-Correlation-ID", Guid.NewGuid().ToString())
//                      .WithBody(new StringContent("{}", Encoding.UTF8, "application/json"));

//    var response = await Http.Send(httpClient, request);
//    return response;
//})
//.WithWarmUpDuration(TimeSpan.FromSeconds(1))
//.WithLoadSimulations(Simulation.KeepConstant(1, TimeSpan.FromMinutes(25))); // זמן מרבי לעדכון מלא של 63K


NBomberRunner
    .RegisterScenarios(
        getLocalitiesScenario,
        getStreetsScenario,
        getLocalityByIdScenario,
        postLocalitiesScenario
        //postStreetsScenario
    )
    .Run();
