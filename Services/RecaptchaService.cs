using System.Text.Json;

namespace ProVMSIT15.Services;

public class RecaptchaService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _secretKey;
    private readonly IHttpContextAccessor _httpContext;

    public RecaptchaService(IHttpClientFactory httpFactory, IConfiguration config, IHttpContextAccessor httpContext)
    {
        _httpFactory = httpFactory;
        _secretKey   = config["ReCaptcha:SecretKey"]!;
        _httpContext  = httpContext;
    }

    public async Task<bool> VerifyAsync(string token)
    {
        // Skip if key is placeholder (not yet configured)
        if (_secretKey == "YOUR_SECRET_KEY_HERE") return true;

        // Bypass on localhost — reCAPTCHA domain validation blocks local dev
        var host = _httpContext.HttpContext?.Request.Host.Host ?? "";
        if (host == "localhost" || host == "127.0.0.1") return true;

        if (string.IsNullOrWhiteSpace(token)) return false;

        var client = _httpFactory.CreateClient();
        var response = await client.PostAsync(
            "https://www.google.com/recaptcha/api/siteverify",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"]   = _secretKey,
                ["response"] = token
            }));

        var json = await response.Content.ReadAsStringAsync();
        var doc  = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("success").GetBoolean();
    }
}
