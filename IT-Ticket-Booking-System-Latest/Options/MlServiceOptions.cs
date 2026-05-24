namespace ITBookingSystem.Options;

public class MlServiceOptions
{
    public const string SectionName = "MlService";

    /// <summary>Base URL of the Python Flask TF-IDF + SVM service, e.g. http://127.0.0.1:5001</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:5001";

    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>When true, uses keyword fallback if Flask is unreachable (demo/review mode).</summary>
    public bool EnableFallback { get; set; } = true;
}
