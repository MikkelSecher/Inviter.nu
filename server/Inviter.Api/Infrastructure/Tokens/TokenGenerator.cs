using System.Security.Cryptography;

namespace Inviter.Api.Infrastructure.Tokens;

public static class TokenGenerator
{
    public static string NewInviteToken() => CreateBase64UrlToken(9);
    public static string NewAdminToken() => CreateBase64UrlToken(32);
    public static string NewImageToken() => CreateBase64UrlToken(12);
    public static string NewInviteeToken() => CreateBase64UrlToken(12);

    private static string CreateBase64UrlToken(int byteLength)
    {
        Span<byte> bytes = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
