namespace ITBookingSystem.Models;

/// <summary>User support bot quick-reply categories (click-only flow).</summary>
public static class SupportCategories
{
    public const string PasswordReset = "Password Reset";
    public const string VpnIssue = "VPN Issue";
    public const string PrinterProblem = "Printer Problem";
    public const string EmailLoginIssue = "Email Login Issue";
    public const string InternetProblem = "Internet Problem";
    public const string SlowSystem = "Slow System";
    public const string SoftwareInstallation = "Software Installation";
    public const string ServerAccessIssue = "Server Access Issue";

    public static readonly IReadOnlyList<string> All =
    [
        PasswordReset,
        VpnIssue,
        PrinterProblem,
        EmailLoginIssue,
        InternetProblem,
        SlowSystem,
        SoftwareInstallation,
        ServerAccessIssue
    ];

    public static ProblemType MapToProblemType(string category) => category switch
    {
        PasswordReset => ProblemType.Software,
        VpnIssue => ProblemType.Network,
        PrinterProblem => ProblemType.Hardware,
        EmailLoginIssue => ProblemType.Software,
        InternetProblem => ProblemType.Network,
        SlowSystem => ProblemType.Software,
        SoftwareInstallation => ProblemType.Software,
        ServerAccessIssue => ProblemType.Network,
        _ => ProblemType.Software
    };
}
