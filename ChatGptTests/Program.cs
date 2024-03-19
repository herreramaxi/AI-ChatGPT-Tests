
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChatGptTests
{
    internal class Program
    {
        public const string CODE = @"from openai import OpenAI
client = OpenAI()

completion = client.chat.completions.create(
  model=""gpt-3.5-turbo"",
  messages=[
    {""role"": ""system"", ""content"": ""You are a poetic assistant, skilled in explaining complex programming concepts with creative flair.""},
    {""role"": ""user"", ""content"": ""Compose a poem that explains the concept of recursion in programming.""}
  ]
)

print(completion.choices[0].message)";
        static async Task Main(string[] args)
        {
            // Set up configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Set up Serilog logger
            Log.Logger = new LoggerConfiguration()
                 .WriteTo.Console()
                 //.WriteTo.File(logFileName, rollingInterval: RollingInterval.Day)
                 .CreateLogger();

            try
            {
                Log.Information("Starting application");

                // Create service provider
                var serviceProvider = new ServiceCollection()
                    .AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<IChatGPTApiExample, ChatGPTApiExample>()
                    .AddLogging(builder =>
                    {
                        builder.AddSerilog();
                    })
                    .BuildServiceProvider();

                // Resolve service
                var myService = serviceProvider.GetService<IChatGPTApiExample>();
                var response = await myService.ExplainCodeAsync(CODE);
                Log.Information($"Response: {response}");

                Log.Information("Application ended successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}