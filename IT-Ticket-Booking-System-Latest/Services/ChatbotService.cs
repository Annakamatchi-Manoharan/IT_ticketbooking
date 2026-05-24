using System.Text.Json;
using ITBookingSystem.Data;
using ITBookingSystem.DTOs;
using ITBookingSystem.Models;
using ITBookingSystem.Repositories;

namespace ITBookingSystem.Services;

public class ChatbotService : IChatbotService
{
    private readonly IChatbotRepository _chatbot;
    private readonly IMlPredictionService _ml;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(IChatbotRepository chatbot, IMlPredictionService ml, ILogger<ChatbotService> logger)
    {
        _chatbot = chatbot;
        _ml = ml;
        _logger = logger;
    }

    public IReadOnlyList<string> GetUserCategories() => SupportCategories.All;

    public async Task<SupportFaqDto?> GetUserGuideAsync(string category, CancellationToken ct = default)
    {
        var faqs = await _chatbot.GetFaqsByCategoryAsync(category, ct);
        var faq = faqs.FirstOrDefault();
        return faq is null ? null : MapFaq(faq);
    }

    public async Task RecordUserIssueViewAsync(int userId, string issueTitle, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueTitle)) return;
        await _chatbot.AddUserHistoryAsync(new UserChatHistory
        {
            UserId = userId,
            SearchedIssue = issueTitle.Trim()
        }, ct);
    }

    public async Task<IReadOnlyList<UserChatHistoryDto>> GetUserRecentHistoryAsync(int userId, CancellationToken ct = default)
    {
        var rows = await _chatbot.GetRecentUserHistoryAsync(userId, 8, ct);
        return rows.Select(h => new UserChatHistoryDto(h.Id, h.SearchedIssue, h.CreatedAt)).ToList();
    }

    public async Task<EngineerChatResponseDto> AskEngineerAssistantAsync(int engineerId, string query, CancellationToken ct = default)
    {
        var prediction = await _ml.PredictAsync(query, ct);

        // Enrich with FAQ steps when ML returns a sparse response.
        if (prediction.FixSteps.Count == 0)
        {
            var faqs = await _chatbot.GetFaqsByCategoryAsync(MapCategoryLabel(prediction.ProblemType), ct);
            var faq = faqs.FirstOrDefault();
            if (faq is not null)
            {
                var steps = DeserializeSteps(faq.StepsJson);
                prediction = prediction with { FixSteps = steps };
            }
        }

        var summary = FormatEngineerSummary(prediction);
        var responseJson = JsonSerializer.Serialize(new
        {
            prediction.PredictedCategory,
            prediction.ProblemType,
            prediction.Confidence,
            prediction.PossibleCauses,
            prediction.FixSteps,
            prediction.RecommendedActions,
            prediction.BestPractices,
            prediction.UsedFallback
        });

        await _chatbot.AddEngineerHistoryAsync(new EngineerChatHistory
        {
            EngineerId = engineerId,
            Query = query.Trim(),
            PredictedCategory = prediction.PredictedCategory,
            BotResponse = responseJson
        }, ct);

        _logger.LogInformation("Engineer {EngineerId} NLP query classified as {Category} (confidence {Confidence:P0})",
            engineerId, prediction.PredictedCategory, prediction.Confidence);

        return new EngineerChatResponseDto(query.Trim(), prediction, summary);
    }

    public async Task EnsureFaqsSeededAsync(CancellationToken ct = default)
    {
        if (await _chatbot.AnyFaqsAsync(ct)) return;
        await _chatbot.SeedFaqsAsync(SupportFaqSeedData.BuildDefaults(), ct);
    }

    private static SupportFaqDto MapFaq(SupportFaq faq) =>
        new(faq.Id, faq.Category, faq.IssueTitle, DeserializeSteps(faq.StepsJson), faq.ProblemType);

    private static IReadOnlyList<string> DeserializeSteps(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string MapCategoryLabel(ProblemType type) => type switch
    {
        ProblemType.Network => SupportCategories.InternetProblem,
        ProblemType.Hardware => SupportCategories.PrinterProblem,
        _ => SupportCategories.SlowSystem
    };

    private static string FormatEngineerSummary(MlPredictionResult p)
    {
        var lines = new List<string>
        {
            $"Predicted category: {p.PredictedCategory} ({p.ProblemType}) — confidence {p.Confidence:P0}" +
            (p.UsedFallback ? " [fallback]" : " [TF-IDF + SVM]")
        };
        if (p.PossibleCauses.Count > 0)
            lines.Add("Possible causes:\n• " + string.Join("\n• ", p.PossibleCauses));
        if (p.FixSteps.Count > 0)
            lines.Add("Step-by-step fixes:\n" + string.Join("\n", p.FixSteps.Select((s, i) => $"{i + 1}. {s}")));
        if (p.RecommendedActions.Count > 0)
            lines.Add("Recommended actions:\n• " + string.Join("\n• ", p.RecommendedActions));
        if (p.BestPractices.Count > 0)
            lines.Add("Best practices:\n• " + string.Join("\n• ", p.BestPractices));
        return string.Join("\n\n", lines);
    }
}
