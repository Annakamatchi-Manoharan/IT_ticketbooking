using System.Security.Claims;
using ITBookingSystem.Models;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITBookingSystem.Controllers;

[Authorize]
[Route("[controller]")]
public class ChatbotController : Controller
{
    private readonly IChatbotService _chatbot;

    public ChatbotController(IChatbotService chatbot) => _chatbot = chatbot;

    /// <summary>Client bootstrap: mode, categories, anti-forgery token name.</summary>
    [HttpGet("config")]
    public IActionResult Config()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var mode = role switch
        {
            nameof(UserRole.Agent) => "engineer",
            nameof(UserRole.User) => "user",
            _ => "none"
        };

        if (mode == "none")
            return Json(new { mode });

        return Json(new
        {
            mode,
            categories = mode == "user" ? _chatbot.GetUserCategories() : Array.Empty<string>(),
            createTicketUrl = Url.Action("Create", "Ticket") ?? "/Ticket/Create"
        });
    }

    [Authorize(Roles = "User")]
    [HttpGet("user/guide")]
    public async Task<IActionResult> UserGuide([FromQuery] string category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
            return BadRequest(new { message = "Category is required." });

        var guide = await _chatbot.GetUserGuideAsync(category.Trim(), ct);
        if (guide is null)
            return NotFound(new { message = "No guide found for this category." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _chatbot.RecordUserIssueViewAsync(userId, guide.IssueTitle, ct);

        return Json(new
        {
            guide.Id,
            guide.Category,
            guide.IssueTitle,
            steps = guide.Steps,
            problemType = guide.ProblemType.ToString(),
            problemTypeValue = (int)guide.ProblemType,
            ticketPrefill = new
            {
                title = guide.Category,
                description = $"Unresolved after self-service: {guide.IssueTitle}",
                problemType = guide.ProblemType.ToString()
            }
        });
    }

    [Authorize(Roles = "User")]
    [HttpGet("user/history")]
    public async Task<IActionResult> UserHistory(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var history = await _chatbot.GetUserRecentHistoryAsync(userId, ct);
        return Json(history);
    }

    [Authorize(Roles = "Agent")]
    [HttpPost("engineer/ask")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EngineerAsk([FromForm] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Please enter a question." });

        var engineerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _chatbot.AskEngineerAssistantAsync(engineerId, query, ct);

        return Json(new
        {
            response.Query,
            summary = response.FormattedSummary,
            prediction = new
            {
                response.Prediction.PredictedCategory,
                problemType = response.Prediction.ProblemType.ToString(),
                response.Prediction.Confidence,
                response.Prediction.PossibleCauses,
                response.Prediction.FixSteps,
                response.Prediction.RecommendedActions,
                response.Prediction.BestPractices,
                response.Prediction.UsedFallback,
                aiPipeline = response.Prediction.UsedFallback ? "Keyword fallback" : "TF-IDF + SVM (Flask)"
            }
        });
    }
}
