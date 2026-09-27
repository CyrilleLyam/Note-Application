namespace server.src.Config;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const int AuthPermitLimit = 5;
    public static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);

    public const string Public = "public";
    public const int PublicPermitLimit = 60;
    public static readonly TimeSpan PublicWindow = TimeSpan.FromMinutes(1);
}
