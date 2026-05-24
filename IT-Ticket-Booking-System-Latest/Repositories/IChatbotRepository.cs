using ITBookingSystem.Models;

namespace ITBookingSystem.Repositories;

public interface IChatbotRepository
{
    Task<List<SupportFaq>> GetFaqsByCategoryAsync(string category, CancellationToken ct = default);
    Task<SupportFaq?> GetFaqByIdAsync(int id, CancellationToken ct = default);
    Task<List<UserChatHistory>> GetRecentUserHistoryAsync(int userId, int take, CancellationToken ct = default);
    Task AddUserHistoryAsync(UserChatHistory entry, CancellationToken ct = default);
    Task<List<EngineerChatHistory>> GetRecentEngineerHistoryAsync(int engineerId, int take, CancellationToken ct = default);
    Task AddEngineerHistoryAsync(EngineerChatHistory entry, CancellationToken ct = default);
    Task<bool> AnyFaqsAsync(CancellationToken ct = default);
    Task SeedFaqsAsync(IEnumerable<SupportFaq> faqs, CancellationToken ct = default);
}
