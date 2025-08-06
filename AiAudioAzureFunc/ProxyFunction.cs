using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AiAudioAzureFunc
{
    public class ProxyFunction
    {
        private readonly HttpClient _client;

        public ProxyFunction(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        [Function("ProxyFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            // OPTIONAL: Validate JWT or App Attest
            var isValid = ValidateJwt(req);
            if (!isValid)
            {
                var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorized.WriteStringAsync("Unauthorized");
                return unauthorized;
            }

            // Parse and relay request
            var content = await req.ReadAsStringAsync();
            var externalRequest = new StringContent(content, Encoding.UTF8, "application/json");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");

            var result = await _client.PostAsync("https://api.openai.com/v1/chat/completions", externalRequest);

            var response = req.CreateResponse(result.StatusCode);
            var responseBody = await result.Content.ReadAsStringAsync();
            await response.WriteStringAsync(responseBody);

            return response;
        }

        private bool ValidateJwt(HttpRequestData req)
        {
            // Implement JWT or App Attest token validation here
            return true; // Stub
        }
    }
    //public class ProxyFunction
    //{
    //    private readonly ILogger<ProxyFunction> _logger;
    //    private readonly HttpClient _client;

    //    public ProxyFunction(ILogger<ProxyFunction> logger, IHttpClientFactory httpClientFactory)
    //    {
    //        _logger = logger;
    //        _client = httpClientFactory.CreateClient();
    //    }

    //    [Function("ProxyFunction")]
    //    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext executionContext)
    //    {
    //        // OPTIONAL: Validate JWT or App Attest
    //        var isValid = ValidateJwt(req);
    //        if (!isValid)
    //        {
    //            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
    //            await unauthorized.WriteStringAsync("Unauthorized");
    //            return unauthorized;
    //        }

    //        // Parse and relay request
    //        var content = await req.ReadAsStringAsync();
    //        var externalRequest = new StringContent(content, Encoding.UTF8, "application/json");

    //        var requestPayload = new
    //        {
    //            model = "gpt-4-turbo", // Or gpt-4-turbo if preferred
    //            messages = new[]
    //            {
    //        new {
    //            role = "user",
    //            content = content
    //        }
    //        },
    //            max_tokens = 3000
    //        };
    //        //var result = await _client.PostAsync("https://api.openai.com/v1/chat/completions", requestPayload);
    //        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
    //        {
    //            Content = externalRequest// new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
    //        };

    //        //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    //        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");
    //        //httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-client/1.0");
    //        _client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-client/1.0");

    //        var response = await _client.SendAsync(request);
    //        //await response.WriteStringAsync(responseBody);

    //        return response;
    //    }

    //    private bool ValidateJwt(HttpRequestData req)
    //    {
    //        // Implement JWT or App Attest token validation here
    //        return true; // Stub
    //    }
    //}
}
