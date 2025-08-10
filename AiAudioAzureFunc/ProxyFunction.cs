using Azure;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace AiAudioAzureFunc
{
    public class ProxyFunction
    {

        private string _apiKey;
        private string _instructionsFileName;
        private string _model;


        public ProxyFunction(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _apiKey = config["OpenAIApiKey"];
            _instructionsFileName = config["instructionsFileName"];
            _model = config["OpenAIAnsweringModel"];
        }

        [Function("ProxyFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            FunctionContext executionContext)
        {
            if (!req.ContentType.StartsWith("multipart/form-data"))
            {
                return new BadRequestObjectResult("Expected multipart/form-data");
            }

            // OPTIONAL: Validate JWT or App Attest
            var isValid = ValidateJwt(req);
            if (!isValid)
            {
                //var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                //await unauthorized.WriteStringAsync("Unauthorized");
                //return unauthorized;
            }

            var contentType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(req.ContentType);
            var boundary = Microsoft.Net.Http.Headers.HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            var reader = new MultipartReader(boundary, req.Body);
            string? audioAsTextContent = null;
            MultipartSection section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
                if (contentDisposition.DispositionType == "form-data" &&
                    contentDisposition.Name.Trim('"') == "file")
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                    var multipartContent = new MultipartFormDataContent();
                    var streamContent = new StreamContent(section.Body);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(section.ContentType ?? "application/octet-stream");
                    multipartContent.Add(streamContent, "file", contentDisposition.FileName.Trim('"'));

                    // Add other required form fields if needed, e.g. model parameter
                    multipartContent.Add(new StringContent("whisper-1"), "model");

                    var response = await client.PostAsync("https://api.openai.com/v1/audio/translations", multipartContent);

                    audioAsTextContent = await response.Content.ReadAsStringAsync();

                }
            }


            string template = await File.ReadAllTextAsync(@$"./{_instructionsFileName}");
            string prompt = template.Replace("{{textFromAudio}}", audioAsTextContent);
            var requestPayload = new
            {
                model = _model, // Or gpt-4-turbo if preferred
                messages = new[]
               {
            new {
                role = "user",
                content = prompt
            }
            },
                max_tokens = 3000
            };

            string jsonString = JsonSerializer.Serialize(requestPayload);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
            };
            HttpClient httpClient = new HttpClient();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-client/1.0");

            var fiveAnswerResponse = await httpClient.SendAsync(request);
            using var responseStream = await fiveAnswerResponse.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(responseStream);

            string responsetext = doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString() ?? string.Empty;

            return new OkObjectResult(responsetext);
        }

        private bool ValidateJwt(HttpRequest req)
        {
            // Implement JWT or App Attest token validation here
            return true; // Stub
        }


    }
   
}
