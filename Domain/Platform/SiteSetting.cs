namespace Portico.Domain.Platform;

// One value the SuperAdmin can change without a release, such as the contact details in the footer.
public class SiteSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public static class SiteSettingKeys
{
    public const string SupportPhone = "SupportPhone";
    public const string SupportEmail = "SupportEmail";
    public const string SupportLocation = "SupportLocation";

    public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>
    {
        [SupportPhone] = "+994 55 299 28 03",
        [SupportEmail] = "support@portico.az",
        [SupportLocation] = "Baku, Azerbaijan"
    };
}
