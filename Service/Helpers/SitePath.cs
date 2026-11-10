using Service.Helpers.Exceptions;

namespace Service.Helpers;

// Slides and videos point at files the site itself serves, such as /assets/rome.jpg or /uploads/slides/ab12.jpg.
// Anything else (another site, a javascript: link, a path that climbs out with "..") is refused, because these
// values are written straight into the home page.
public static class SitePath
{
    public const int MaxLength = 300;

    public static string Require(string? value, string what)
    {
        var path = (value ?? "").Trim();
        if (path.Length == 0) throw new BadRequestException($"Choose the {what}.");
        if (path.Length > MaxLength) throw new BadRequestException($"The {what} path must be under {MaxLength} characters.");
        if (!path.StartsWith('/') || path.StartsWith("//") || path.Contains('\\') || path.Contains("..")
            || path.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '"' or '\'' or '<' or '>' or '(' or ')'))
            throw new BadRequestException($"The {what} must be a file on this site, such as /assets/rome.jpg.");
        return path;
    }
}
