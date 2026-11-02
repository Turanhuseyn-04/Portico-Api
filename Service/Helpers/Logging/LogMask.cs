namespace Service.Helpers.Logging;

// Logs name an account without spelling out its email: "turan@gmail.com" is written as "t***@gmail.com".
// The first letter and the domain are enough to tell accounts apart while debugging, and a log file that leaks
// does not leak the guest list. Passwords, tokens and card details are never passed to a logger at all.
public static class LogMask
{
    public static string Email(string? email)
    {
        var value = (email ?? "").Trim();
        if (value.Length == 0) return "(none)";

        var at = value.LastIndexOf('@');
        if (at <= 0) return "***";
        return $"{value[0]}***{value[at..]}";
    }
}
