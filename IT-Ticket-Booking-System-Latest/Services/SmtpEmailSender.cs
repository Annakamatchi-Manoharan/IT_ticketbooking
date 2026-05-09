using System.Net;
using System.Net.Mail;
using ITBookingSystem.Options;
using Microsoft.Extensions.Options;

namespace ITBookingSystem.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> opts, ILogger<SmtpEmailSender> logger)
    {
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.SmtpHost))
        {
            _logger.LogInformation("Email skipped (SMTP disabled): To={To} Subject={Subject}", toEmail, subject);
            return;
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(_opts.FromAddress, _opts.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(toEmail);

        using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
        {
            EnableSsl = _opts.UseSsl,
            Credentials = string.IsNullOrEmpty(_opts.User)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_opts.User, _opts.Password)
        };

        await client.SendMailAsync(msg, ct);
    }
}
