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
        try
        {
            var payload = new
            {
                start_date = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                velocity = velocity,
                remaining_complexity = remainingComplexity
            };

            // This will post to /predict on the BaseAddress configured via HttpClient in DependencyInjection
            var response = await _httpClient.PostAsJsonAsync("http://127.0.0.1:8000/predict", payload);

            if (response.IsSuccessStatusCode)
            {
                var resultText = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(resultText);
                var dateString = jsonDoc.RootElement.GetProperty("predicted_date").GetString();

                if (DateTime.TryParse(dateString, out var predictedDate))
                {
                    return predictedDate;
                }
            }

            Console.WriteLine($"[AiPredictionProvider] AI Service call failed or returned invalid date. Status: {response.StatusCode}. Falling back to simplistic calculation.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiPredictionProvider] Exception during AI Service call: {ex.Message}. Falling back to simplistic calculation.");
        }

        // Fallback simplistic calculation if the AI payload parse fails or service is unreachable
        int daysNeeded = (int)Math.Ceiling(remainingComplexity / (velocity > 0 ? velocity : 1.0));
        return DateTime.UtcNow.AddDays(daysNeeded);
    }
}
