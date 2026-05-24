using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Options;
using Microsoft.Extensions.Options;

namespace ITBookingSystem.Services;

/// <summary>
/// Integrates with the Python Flask service that runs TF-IDF vectorization + SVM classification.
/// Falls back to keyword rules when the service is offline (review/demo mode).
/// </summary>
public class MlPredictionService : IMlPredictionService
{
    private readonly HttpClient _http;
    private readonly MlServiceOptions _options;
    private readonly ILogger<MlPredictionService> _logger;

    public MlPredictionService(HttpClient http, IOptions<MlServiceOptions> options, ILogger<MlPredictionService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MlPredictionResult> PredictAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BuildFallback("General", ProblemType.Software, 0.5, query);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var response = await _http.PostAsJsonAsync("/api/predict", new { text = query.Trim() }, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ML service returned {StatusCode}", response.StatusCode);
                return FallbackOrThrow(query);
            }

            var payload = await response.Content.ReadFromJsonAsync<FlaskPredictResponse>(cancellationToken: cts.Token);
            if (payload is null)
                return FallbackOrThrow(query);

            var problemType = ParseProblemType(payload.ProblemType);
            return new MlPredictionResult(
                payload.PredictedCategory ?? problemType.ToString(),
                problemType,
                payload.Confidence,
                payload.PossibleCauses ?? [],
                payload.FixSteps ?? [],
                payload.RecommendedActions ?? [],
                payload.BestPractices ?? [],
                UsedFallback: false);
        }
        catch (Exception ex) when (_options.EnableFallback)
        {
            _logger.LogInformation(ex, "ML service unavailable; using keyword fallback.");
            return KeywordFallback(query);
        }
    }

    private MlPredictionResult FallbackOrThrow(string query)
    {
        if (_options.EnableFallback)
            return KeywordFallback(query);
        throw new InvalidOperationException("AI prediction service is unavailable.");
    }

    private static MlPredictionResult KeywordFallback(string query)
    {
        var lower = query.ToLowerInvariant();
        if (lower.Contains("vpn") || lower.Contains("network") || lower.Contains("internet") || lower.Contains("wifi"))
            return BuildFallback("Network", ProblemType.Network, 0.72, query);
        if (lower.Contains("printer") || lower.Contains("hardware") || lower.Contains("monitor") || lower.Contains("keyboard"))
            return BuildFallback("Hardware", ProblemType.Hardware, 0.7, query);
        if (lower.Contains("server") || lower.Contains("database") || lower.Contains("host"))
            return BuildFallback("Network / Server", ProblemType.Network, 0.68, query);
        return BuildFallback("Software", ProblemType.Software, 0.65, query);
    }

    private static MlPredictionResult BuildFallback(string label, ProblemType type, double confidence, string query)
    {
        var causes = new List<string>
        {
            "Misconfiguration or expired credentials",
            "Pending updates or service outage",
            "Local client/cache corruption"
        };
        var steps = new List<string>
        {
            "Gather exact error message, time, and affected system.",
            "Restart the affected application and device.",
            "Verify VPN/network connectivity if remote.",
            "Compare with known-good user or device.",
            "Escalate with logs if issue reproduces."
        };
        if (query.Contains("slow", StringComparison.OrdinalIgnoreCase))
        {
            causes.Add("High resource usage or low disk space");
            steps.Insert(1, "Close heavy apps and check Task Manager for CPU/RAM spikes.");
        }

        return new MlPredictionResult(
            label,
            type,
            confidence,
            causes,
            steps,
            ["Document reproduction steps", "Attach screenshots/logs to the ticket"],
            ["Apply least-privilege access", "Validate after change in a test window"],
            UsedFallback: true);
    }

    private static ProblemType ParseProblemType(string? value) =>
        Enum.TryParse<ProblemType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : ProblemType.Software;

    private sealed class FlaskPredictResponse
    {
        [JsonPropertyName("predicted_category")]
        public string? PredictedCategory { get; set; }

        [JsonPropertyName("problem_type")]
        public string? ProblemType { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("possible_causes")]
        public List<string>? PossibleCauses { get; set; }

        [JsonPropertyName("fix_steps")]
        public List<string>? FixSteps { get; set; }

        [JsonPropertyName("recommended_actions")]
        public List<string>? RecommendedActions { get; set; }

        [JsonPropertyName("best_practices")]
        public List<string>? BestPractices { get; set; }
    }
}
