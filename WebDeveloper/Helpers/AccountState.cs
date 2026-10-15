using WebDeveloper.Models.Entities;

namespace WebDeveloper.Helpers;

public static class AccountState
{
    // VerificationCode also stores password reset codes. An admin ban reason
    // takes precedence over a leftover code from an earlier reset request.
    public static bool AwaitingEmailVerification(User user) => user.IsActive != true
        && string.IsNullOrWhiteSpace(user.ReasonBanned) && user.VerificationCode != null;

    public static void ClearOneTimeCode(User user)
    {
        user.VerificationCode = null;
        user.CodeExpiry = null;
    }
}
