namespace ITBookingSystem.Services;

/// <summary>Logs only — use when SMTP is disabled in configuration.</summary>
public class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("Email (dev/null): To={To} Subject={Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
