namespace __PROJECT__.Infrastructure.Options;

public sealed class ApiActivityLogOptions
{
    public const string SectionName = "AuditActivity";

    public bool Enabled { get; set; } = true;
    public int QueueCapacity { get; set; } = 10000;
    public int BatchSize { get; set; } = 100;
    public int FlushIntervalSeconds { get; set; } = 2;
    public string[] ExcludedPaths { get; set; } =
    [
        "/health",
        "/swagger",
        "/hangfire",
        "/favicon.ico",
    ];
}
