using ITBookingSystem.Data;
using ITBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBookingSystem.Repositories;

public class ChatbotRepository : IChatbotRepository
{
    private readonly AppDbContext _db;

    public ChatbotRepository(AppDbContext db) => _db = db;

    public Task<List<SupportFaq>> GetFaqsByCategoryAsync(string category, CancellationToken ct = default) =>
        _db.SupportFaqs.AsNoTracking()
            .Where(f => f.Category == category)
            .OrderBy(f => f.IssueTitle)
            .ToListAsync(ct);

    public Task<SupportFaq?> GetFaqByIdAsync(int id, CancellationToken ct = default) =>
        _db.SupportFaqs.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<List<UserChatHistory>> GetRecentUserHistoryAsync(int userId, int take, CancellationToken ct = default) =>
        _db.UserChatHistories.AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddUserHistoryAsync(UserChatHistory entry, CancellationToken ct = default)
    {
        _db.UserChatHistories.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<EngineerChatHistory>> GetRecentEngineerHistoryAsync(int engineerId, int take, CancellationToken ct = default) =>
        _db.EngineerChatHistories.AsNoTracking()
            .Where(h => h.EngineerId == engineerId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddEngineerHistoryAsync(EngineerChatHistory entry, CancellationToken ct = default)
    {
        _db.EngineerChatHistories.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> AnyFaqsAsync(CancellationToken ct = default) =>
        _db.SupportFaqs.AnyAsync(ct);

    public async Task SeedFaqsAsync(IEnumerable<SupportFaq> faqs, CancellationToken ct = default)
    {
        _db.SupportFaqs.AddRange(faqs);
        await _db.SaveChangesAsync(ct);
    }
}
