using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NeuroPlan.Application.Interfaces;
using System;

namespace NeuroPlan.Infrastructure.ExternalServices;

public class AiPredictionProvider : IAiPredictionProvider
{
    private readonly HttpClient _httpClient;

    public AiPredictionProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DateTime> PredictCompletionDateAsync(DateTime startDate, double velocity, int remainingComplexity)
    {
        var payload = new
        {
            start_date = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            velocity = velocity,
            remaining_complexity = remainingComplexity
        };

        // This will post to /predict on the BaseAddress configured via HttpClient in DependencyInjection
        var response = await _httpClient.PostAsJsonAsync("predict", payload);

        response.EnsureSuccessStatusCode();

        var resultText = await response.Content.ReadAsStringAsync();

        try
        {
            var jsonDoc = JsonDocument.Parse(resultText);
            var dateString = jsonDoc.RootElement.GetProperty("predicted_date").GetString();
            return DateTime.Parse(dateString!);
        }
        catch
        {
            // Fallback simplistic calculation if the AI payload parse fails or isn't completely hooked up yet
            int daysNeeded = (int)Math.Ceiling(remainingComplexity / velocity);
            return DateTime.UtcNow.AddDays(daysNeeded);
        }
    }
}
