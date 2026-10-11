using System.Security.Cryptography;
using System.Text;

namespace WebDeveloper.Security;

public static class TokenBlacklistKey
{
    // A JWT can exceed the blacklist column's 500-character limit when it
    // includes an avatar URL. A SHA-256 key is fixed length and avoids storing
    // the bearer credential itself.
    public static string FromToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
