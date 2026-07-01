namespace Pulse.Services;

/// <summary>
/// Environment endpoints for local development against the PulseApi Aspire AppHost.
/// The API port is pinned by Pulse.Api.ApiService/Properties/launchSettings.json;
/// the Supabase values are printed by the AppHost (Aspire dashboard → "supabase"
/// resource → endpoints/environment, or the ConnectionStrings__supabase__Url/__Key
/// variables injected into "apiservice").
/// </summary>
public static class AppConfig
{
    private const string ApiHost = "http://192.168.1.10:7090";

    /// <summary>Supabase Kong gateway (auth lives at {url}/auth/v1).</summary>
    private const string SupabaseHost = "http://192.168.1.10:7089";

    /// <summary>Supabase anon key — copy from the Aspire dashboard ("apiservice" env: ConnectionStrings__supabase__Key).</summary>
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";

    /// <summary>
    /// RevenueCat public SDK key (platform-specific in production: the Apple key on iOS, the Google
    /// key on Android). Placeholder until the RevenueCat dashboard is configured; the SDK only takes
    /// payment — token balances live in the Pulse DB. Safe to ship (it's a publishable key).
    /// </summary>
    public const string RevenueCatApiKey = "REVENUECAT_PUBLIC_SDK_KEY_PLACEHOLDER";

    public static string ApiBaseUrl => ForDevice(ApiHost);
    public static string SupabaseUrl => ForDevice(SupabaseHost);

    /// <summary>The Android emulator reaches the host machine via 10.0.2.2 rather than localhost.</summary>
    private static string ForDevice(string url) =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? url.Replace("localhost", "10.0.2.2")
            : url;
}
