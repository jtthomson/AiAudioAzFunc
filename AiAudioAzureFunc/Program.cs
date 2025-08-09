//using Microsoft.Azure.Functions.Worker.Builder;
//using Microsoft.Extensions.Hosting;

//var builder = FunctionsApplication.CreateBuilder(args);

//builder.ConfigureFunctionsWebApplication();

//// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
//// builder.Services
////     .AddApplicationInsightsTelemetryWorkerService()
////     .ConfigureFunctionsApplicationInsights();

//builder.Build().Run();

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
     .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddHttpClient(); // Registers IHttpClientFactory
    })
    .Build();

host.Run();
