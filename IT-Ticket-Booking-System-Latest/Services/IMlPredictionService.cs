using ITBookingSystem.DTOs;

namespace ITBookingSystem.Services;

public interface IMlPredictionService
{
    Task<MlPredictionResult> PredictAsync(string query, CancellationToken ct = default);
}
