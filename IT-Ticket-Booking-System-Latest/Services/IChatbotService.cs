using ITBookingSystem.DTOs;

namespace ITBookingSystem.Services;

public interface IChatbotService
{
    IReadOnlyList<string> GetUserCategories();
    Task<SupportFaqDto?> GetUserGuideAsync(string category, CancellationToken ct = default);
    Task RecordUserIssueViewAsync(int userId, string issueTitle, CancellationToken ct = default);
    Task<IReadOnlyList<UserChatHistoryDto>> GetUserRecentHistoryAsync(int userId, CancellationToken ct = default);
    Task<EngineerChatResponseDto> AskEngineerAssistantAsync(int engineerId, string query, CancellationToken ct = default);
    Task EnsureFaqsSeededAsync(CancellationToken ct = default);
}
