using System.Text.Json;
using ITBookingSystem.Models;

namespace ITBookingSystem.Data;

public static class SupportFaqSeedData
{
    public static IReadOnlyList<SupportFaq> BuildDefaults() =>
    [
        Faq(SupportCategories.PasswordReset, "Reset domain password", ProblemType.Software,
        [
            "Open the company password portal from the intranet home page.",
            "Click Forgot password and verify your identity with MFA.",
            "Set a new password that meets complexity rules (12+ chars, mixed case, number, symbol).",
            "Sign out of all sessions, wait 2 minutes, then sign in again.",
            "If sync errors appear in Outlook, restart the Microsoft 365 apps."
        ]),
        Faq(SupportCategories.VpnIssue, "VPN will not connect", ProblemType.Network,
        [
            "Confirm you have internet access without VPN (open https://portal.office.com).",
            "Restart the VPN client and choose the closest gateway region.",
            "Clear saved credentials in the VPN app and sign in again.",
            "Disable third-party firewalls temporarily and retry.",
            "If error persists, capture the client log and escalate."
        ]),
        Faq(SupportCategories.PrinterProblem, "Printer offline", ProblemType.Hardware,
        [
            "Verify the printer is powered on and shows Ready on the panel.",
            "Remove and re-add the printer from Windows Printers & scanners.",
            "Print a test page from printer properties.",
            "Check the print queue for stuck jobs and clear them.",
            "For network printers, ping the printer IP from command prompt."
        ]),
        Faq(SupportCategories.EmailLoginIssue, "Cannot sign in to email", ProblemType.Software,
        [
            "Confirm caps lock is off and use the full corporate email address.",
            "Reset password via the self-service portal if authentication fails.",
            "In Outlook, run Office repair from Apps & features.",
            "Remove and re-add the profile if repeated token errors occur.",
            "Check service health dashboard for Exchange outages."
        ]),
        Faq(SupportCategories.InternetProblem, "No internet access", ProblemType.Network,
        [
            "Check if Wi-Fi is connected or Ethernet link lights are active.",
            "Run Windows Network Troubleshooter.",
            "Release and renew IP: ipconfig /release then ipconfig /renew.",
            "Try another SSID or a wired dock connection.",
            "If only one site fails, flush DNS: ipconfig /flushdns."
        ]),
        Faq(SupportCategories.SlowSystem, "Laptop running slowly", ProblemType.Software,
        [
            "Restart the laptop and install pending Windows updates.",
            "Close unused browser tabs and heavy applications.",
            "Check disk space; keep at least 15% free on C: drive.",
            "Run a quick antivirus scan.",
            "Review Task Manager for high CPU or memory processes."
        ]),
        Faq(SupportCategories.SoftwareInstallation, "Request software install", ProblemType.Software,
        [
            "Open Software Center (or Company Portal) and search for the app.",
            "If not listed, raise a ticket with business justification.",
            "For admin rights requests, attach manager approval.",
            "Do not install unsigned installers from email attachments.",
            "After install, reboot once before reporting failure."
        ]),
        Faq(SupportCategories.ServerAccessIssue, "Cannot access server", ProblemType.Network,
        [
            "Confirm VPN is connected when accessing servers remotely.",
            "Verify your AD group membership for the target server role.",
            "Test connectivity: ping or Test-NetConnection to host and port.",
            "Check if maintenance window is published on the status page.",
            "Provide server name, error screenshot, and timestamp for escalation."
        ])
    ];

    private static SupportFaq Faq(string category, string title, ProblemType problemType, string[] steps) =>
        new()
        {
            Category = category,
            IssueTitle = title,
            ProblemType = problemType,
            StepsJson = JsonSerializer.Serialize(steps),
            CreatedAt = DateTime.UtcNow
        };
}
