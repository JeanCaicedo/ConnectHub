using System.Text.RegularExpressions;

namespace ConnectHub.API.Helpers;

// Lógica pura de extracción de hashtags, aislada para poder testearla
// sin base de datos ni controllers.
public static class HashtagParser
{
    private static readonly Regex HashtagRegex = new(@"#(\w+)", RegexOptions.Compiled);

    // Devuelve los hashtags únicos, en minúsculas y sin la almohadilla.
    public static IReadOnlyList<string> Extract(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        return HashtagRegex.Matches(content)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
