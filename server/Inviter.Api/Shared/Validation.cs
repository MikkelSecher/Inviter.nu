namespace Inviter.Api.Shared;

public static class Validation
{
    public static bool LooksLikeEmail(string s)
    {
        if (s.Contains(' ')) return false;
        var at = s.IndexOf('@');
        return at > 0 && at < s.Length - 3 && s.IndexOf('.', at) > at + 1;
    }

    public static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static string? NormalizeOrganizerEmail(string? value) => NormalizeOptional(value);
}
