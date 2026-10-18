namespace Portico.Domain.Properties;

public class Property
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int? Stars { get; set; }
    public string Street { get; set; } = "";
    public string Area { get; set; } = "";
    public string City { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Country { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal Score { get; set; }
    public int ReviewCount { get; set; }
    public string Summary { get; set; } = "";
    public string DescriptionJson { get; set; } = "[]";
    public string HighlightsJson { get; set; } = "[]";
    public string ScoresJson { get; set; } = "{}";
    public string PopularJson { get; set; } = "[]";
    public string AmenityGroupsJson { get; set; } = "[]";
    public string GalleryJson { get; set; } = "[]";
    public string NearbyJson { get; set; } = "[]";
    public string? PoliciesJson { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<PropertyPhoto> Photos { get; set; } = new List<PropertyPhoto>();
}
