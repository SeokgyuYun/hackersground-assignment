using Azure;
using Azure.AI.OpenAI;

using Microsoft.AspNetCore.Mvc;

using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddScoped<AzureOpenAIClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = new Uri(config["OpenAI:Endpoint"]);
    var credential = new AzureKeyCredential(config["OpenAI:ApiKey"]);
    var client = new AzureOpenAIClient(endpoint, credential);

    return client;
});

builder.Services.AddScoped<QNAService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/summarise", async ([FromBody] AnswerRequest req, QNAService service) =>
{
    var answer = await service.AnswerAsync(req);
    return answer;
})
.WithName("GetAnswer")
.WithOpenApi();


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record AnswerRequest(string question, string questionLanguageCode, string? answerLanguageCode);

internal class QNAService(AzureOpenAIClient openai, IConfiguration config)
{
    private readonly AzureOpenAIClient _openai = openai ?? throw new ArgumentNullException(nameof(openai));
    private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    public async Task<string> AnswerAsync(AnswerRequest req)
    {
        string caption = req.question;
        var chat = this._openai.GetChatClient(this._config["OpenAI:DeploymentName"]);
        var messages = new List<ChatMessage>()
        {
            new SystemChatMessage(this._config["Prompt:System"]),
            new SystemChatMessage($"Here's the answer. Answer the question in the given language code of \"{req.answerLanguageCode}\"."),
            new UserChatMessage(caption),
        };
        ChatCompletionOptions options = new()
        {
            MaxTokens = int.TryParse(this._config["Prompt:MaxTokens"], out var maxTokens) ? maxTokens : 3000,
            Temperature = float.TryParse(this._config["Prompt:Temperature"], out var temperature) ? temperature : 0.7f,
        };

        var response = await chat.CompleteChatAsync(messages, options).ConfigureAwait(false);
        var answer = response.Value.Content[0].Text;

        return answer;
    }
}
