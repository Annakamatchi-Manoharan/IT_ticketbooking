using ITBookingSystem.Models;

namespace ITBookingSystem.DTOs;

public record SupportFaqDto(int Id, string Category, string IssueTitle, IReadOnlyList<string> Steps, ProblemType ProblemType);

public record UserChatHistoryDto(int Id, string SearchedIssue, DateTime CreatedAt);

public record MlPredictionResult(
    string PredictedCategory,
    ProblemType ProblemType,
    double Confidence,
    IReadOnlyList<string> PossibleCauses,
    IReadOnlyList<string> FixSteps,
    IReadOnlyList<string> RecommendedActions,
    IReadOnlyList<string> BestPractices,
    bool UsedFallback);

public record EngineerChatResponseDto(
    string Query,
    MlPredictionResult Prediction,
    string FormattedSummary);
