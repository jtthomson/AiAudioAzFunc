using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

//Application Insights Logging TODO..  
namespace AiAudioAzureFunc
{
    public class ProxyFunction
    {
        private readonly ILogger<ProxyFunction> _logger;
        private string _apiKey;
        private string _instructionsFileName;
        private string _model;
        private readonly OpenAIClient _client;


        public ProxyFunction(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ProxyFunction> logger)
        {
            _apiKey = config["OpenAIApiKey"];
            _instructionsFileName = config["instructionsFileName"];
            _model = config["OpenAIAnsweringModel"];
            _logger = logger;
            _client = new OpenAIClient(_apiKey);
        }

        [Function("ProxyFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var totalStopwatch = new Stopwatch();
            totalStopwatch.Start();
            _logger.LogInformation("******* Proxy Function Entered *********");
            _logger.LogInformation("Validate JWT");
            // OPTIONAL: Validate JWT or App Attest
            var isValid = ValidateJwt(req);
            if (!isValid)
            {
                //var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                //await unauthorized.WriteStringAsync("Unauthorized");
                //return unauthorized;
            }

            List<ChatMessage> extractQuestionMessages = new List<ChatMessage>();
            List<ChatMessage> getAnswerMessages = new List<ChatMessage>();
            string restatedQuestionText = string.Empty;
            string fullTextFromAudio = string.Empty;

            try
            {
                var transcriptionStopwatch = new Stopwatch();
                transcriptionStopwatch.Start();
                var stt = _client.GetAudioClient("gpt-4o-mini-transcribe").TranscribeAudioAsync(req.Body, "fromUnity.wav");
                var transcription = await stt;
                transcriptionStopwatch.Stop();
                fullTextFromAudio = transcription.Value.Text;
                _logger.LogInformation($"Seconds to transcribe: {Math.Round(transcriptionStopwatch.Elapsed.TotalSeconds, 1)}");
                _logger.LogInformation(fullTextFromAudio);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }

            extractQuestionMessages = new List<ChatMessage>
                {
                    new SystemChatMessage(
                        "Repeat the last user question clearly. If it is vague, expand it with inferred context."
                    ),
                    new UserChatMessage(fullTextFromAudio)
                };

            var extractQuestionStropwatch = new Stopwatch();
            extractQuestionStropwatch.Start();
            var ExtractQuestionResponse = await _client.GetChatClient("gpt-4o-mini").CompleteChatAsync(extractQuestionMessages.ToArray());
            extractQuestionStropwatch.Stop();
            _logger.LogInformation($"Seconds to Extract the Question: {Math.Round(extractQuestionStropwatch.Elapsed.TotalSeconds, 1)}");
            string questionRestated = ExtractQuestionResponse.Value.Content[0].Text;

            getAnswerMessages = new List<ChatMessage>
                {
                    new SystemChatMessage(
                        "You are an expert interviewee. " +
                        "Respond in <16 words, easy to speak, rapid style. "
                    ),
                    new UserChatMessage(questionRestated)
                };

            var chatStropwatch = new Stopwatch();
            chatStropwatch.Start();
            var chatResponse = await _client.GetChatClient("gpt-4o-mini").CompleteChatAsync(getAnswerMessages.ToArray());
            chatStropwatch.Stop();
            _logger.LogInformation($"Seconds to Get Answer: {Math.Round(chatStropwatch.Elapsed.TotalSeconds, 1)}");
            string answer = chatResponse.Value.Content[0].Text;

            var ttsStropwatch = new Stopwatch();
            ttsStropwatch.Start();
            var ttsResponse = await _client.GetAudioClient("gpt-4o-mini-tts").GenerateSpeechAsync(answer, OpenAI.Audio.GeneratedSpeechVoice.Echo);
            ttsStropwatch.Stop();
            _logger.LogInformation($"Seconds for TTS: {Math.Round(ttsStropwatch.Elapsed.TotalSeconds, 1)}");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("X-Question1", questionRestated);
            response.Headers.Add("X-Answer1", answer);

            response.Headers.Add("Content-Type", "audio/mpeg");
            await response.Body.WriteAsync(ttsResponse.Value);
            totalStopwatch.Stop();
            _logger.LogInformation($"Total Seconds to respond: {Math.Round(totalStopwatch.Elapsed.TotalSeconds, 1)}");
            return response;
        }

        private bool ValidateJwt(HttpRequestData req)
        {
            _logger.LogInformation("JWT Not Implemented");
            // Implement JWT or App Attest token validation here
            return true; // Stub
        }


    }

}


//var contentType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(req.ContentType);
//var boundary = Microsoft.Net.Http.Headers.HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
//var reader = new MultipartReader(boundary, req.Body);
//string? audioAsTextContent = null;
//MultipartSection section;
//while ((section = await reader.ReadNextSectionAsync()) != null)
//{
//    var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
//    if (contentDisposition.DispositionType == "form-data" &&
//        contentDisposition.Name.Trim('"') == "file")
//    {
//        var translationsstopwatch = Stopwatch.StartNew();
//        _logger.LogInformation("translations started at {time}", DateTime.UtcNow);

//        using var client = new HttpClient();
//        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

//        var multipartContent = new MultipartFormDataContent();
//        var streamContent = new StreamContent(section.Body);
//        streamContent.Headers.ContentType = new MediaTypeHeaderValue(section.ContentType ?? "application/octet-stream");
//        multipartContent.Add(streamContent, "file", contentDisposition.FileName.Trim('"'));

//        // Add other required form fields if needed, e.g. model parameter
//        multipartContent.Add(new StringContent("whisper-1"), "model");

//        var response = await client.PostAsync("https://api.openai.com/v1/audio/translations", multipartContent);

//        audioAsTextContent = await response.Content.ReadAsStringAsync();
//        translationsstopwatch.Stop();
//        _logger.LogInformation("translations completed in {elapsed} ms", translationsstopwatch.ElapsedMilliseconds);


//    }
//}


//var rootPath = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot")  // Local debug
//   ?? Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "site", "wwwroot"); // Azure

//var filePath = Path.Combine(rootPath, _instructionsFileName);

//string template = await File.ReadAllTextAsync(filePath);
//string prompt = template.Replace("{{textFromAudio}}", audioAsTextContent);

//var chatResponse = await _client.GetChatClient("gpt-4o").CompleteChat()

//            var requestPayload = new
//                                 {
//                                     model = _model, // Or gpt-4-turbo if preferred
//                                     messages = new[]
//               {
//            new {
//                role = "user",
//                content = prompt
//            }
//            },
//                                     max_tokens = 3000
//                                 };
//var stopwatch = Stopwatch.StartNew();
//_logger.LogInformation("completions started at {time}", DateTime.UtcNow);
//string jsonString = JsonSerializer.Serialize(requestPayload);
//var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
//{
//    Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json")
//};
//HttpClient httpClient = new HttpClient();
//request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
//httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-client/1.0");

//var fiveAnswerResponse = await httpClient.SendAsync(request);
//using var responseStream = await fiveAnswerResponse.Content.ReadAsStreamAsync();
//using var doc = await JsonDocument.ParseAsync(responseStream);
//stopwatch.Stop();
//_logger.LogInformation("completions completed in {elapsed} ms", stopwatch.ElapsedMilliseconds);

//string responsetext = doc.RootElement
//          .GetProperty("choices")[0]
//          .GetProperty("message")
//          .GetProperty("content")
//          .GetString() ?? string.Empty;
//_logger.LogInformation(responsetext);
//return new OkObjectResult(responsetext);