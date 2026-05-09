using System.Text;

namespace ITBookingSystem.Services;

/// <summary>Plain-text input hardening (trim, length, control chars).</summary>
public static class InputSanitizer
{
    public static string SanitizePlainText(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input.Trim())
        {
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') continue;
            sb.Append(c);
            if (sb.Length >= maxLength) break;
        }

        return sb.ToString();
    }
}
