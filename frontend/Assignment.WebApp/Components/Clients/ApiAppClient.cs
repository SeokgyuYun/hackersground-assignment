namespace Assignment.WebApp.Clients;

public interface IApiAppClient
{
    Task<List<ApiAppClient.WeatherForecast>> WeatherForecastAsync();
    Task<string> AnswerAsync(string question, string questionLanguageCode, string answerLanguageCode);
}

public class ApiAppClient(HttpClient http) : IApiAppClient
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));

    public async Task<string> AnswerAsync(string question, string questionLanguageCode, string answerLanguageCode)
    {
        using var response = await _http.PostAsJsonAsync(
            "answer",
            new { question, questionLanguageCode, answerLanguageCode }).ConfigureAwait(false);

        var answer = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return answer;
    }

    public async Task<List<WeatherForecast>> WeatherForecastAsync()
    {
        using var response = await _http.GetAsync("weatherforecast").ConfigureAwait(false);

        var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecast>>().ConfigureAwait(false);
        return forecasts ?? [];
    }

    public class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}