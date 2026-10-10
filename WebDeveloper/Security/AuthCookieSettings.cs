namespace WebDeveloper.Security;

public sealed class AuthCookieSettings
{
    public bool Secure { get; }

    public AuthCookieSettings(IConfiguration configuration, IWebHostEnvironment environment)
    {
        // HTTP deployment requires an explicit opt-in. Keep production HTTPS safe by default.
        Secure = !environment.IsDevelopment() &&
            !configuration.GetValue<bool>("Authentication:AllowInsecureCookies");
    }
}
