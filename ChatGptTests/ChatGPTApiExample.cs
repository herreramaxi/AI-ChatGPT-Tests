using Ardalis.Result;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChatGptTests
{
    public interface IChatGPTApiExample {
        Task<Result<string>> ExplainCodeAsync(string codeSnippet);
    }
    public class ChatGPTApiExample: IChatGPTApiExample
    {
        private readonly ILogger<ChatGPTApiExample> _logger;
        private readonly IConfiguration _configuration;

        public ChatGPTApiExample(ILogger<ChatGPTApiExample> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<Result<string>> ExplainCodeAsync(string codeSnippet)
        {
            var prompt = "Please explain the following code:";
            var apikey = _configuration["API_KEY"];
            var apiUrl = "https://api.openai.com/v1/chat/completions";
            var model = "gpt-4-turbo-preview";
            var temperature = 0.3;

            var messages = new[] {
                new
                {
                    role= "user",
                    content=$"{prompt}\n\n{codeSnippet}"
                }
            };

            var data = new
            {
                model = model,
                messages = messages,
                temperature = temperature
            };

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey.Trim());
            //httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer: {apikey}");
            var json = JsonSerializer.Serialize(data);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(apiUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ChatGPT API response did not contain expected content");
                    return Result.Error("ChatGPT description could not be loaded");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                var choicesElement = doc.RootElement.GetProperty("choices");
                var messageObject = choicesElement[0].GetProperty("message");
                var content = messageObject.GetProperty("content").GetString();

                if (content is null)
                {
                    _logger.LogError("ChatGPT API response did not contain expected content");
                    return Result.Error("ChatGPT description could not be loaded");
                }

                return Result.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ChatGPT error occurred: {ex.Message}");
                return Result.Error("ChatGPT description could not be loaded");
            }
        }
    }
}
