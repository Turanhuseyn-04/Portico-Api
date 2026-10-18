namespace Service.Helpers.DTOs.Admin;

public sealed record SlideDto(int Id, string City, string Country, string Image, int Stays, int Order);

public sealed class SlideRequest
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Image { get; set; } = "";
    public int Stays { get; set; }
    public int Order { get; set; }
}

public sealed record VideoDto(int Id, string Src, int Order);

public sealed class VideoRequest
{
    public string Src { get; set; } = "";
    public int Order { get; set; }
}

public sealed record ReviewDto(
    int Id,
    string PropertyId,
    string? RoomId,
    string Author,
    string Country,
    string Traveller,
    int Nights,
    DateOnly Date,
    decimal Score,
    string Title,
    string Liked,
    string Disliked);

public sealed class ReviewRequest
{
    public string PropertyId { get; set; } = "";
    public string? RoomId { get; set; }
    public string Author { get; set; } = "";
    public string Country { get; set; } = "";
    public string Traveller { get; set; } = "";
    public int Nights { get; set; }
    public DateOnly Date { get; set; }
    public decimal Score { get; set; }
    public string Title { get; set; } = "";
    public string Liked { get; set; } = "";
    public string Disliked { get; set; } = "";
}

public sealed record PhotoDto(int Id, string Src, string Alt, int Order);

public sealed class PhotoRequest
{
    public string Src { get; set; } = "";
    public string Alt { get; set; } = "";
    public int Order { get; set; }
}

public sealed record AdminRoomDto(string Id, string Name, decimal Price, int Sleeps, bool Refundable, bool Breakfast, int Inventory);

public sealed class AdminRoomRequest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Sleeps { get; set; }
    public bool Refundable { get; set; }
    public bool Breakfast { get; set; }
    public int Inventory { get; set; }
}

public sealed record AdminPropertyDto(
    string Id,
    string Name,
    string Type,
    int? Stars,
    string Street,
    string Area,
    string City,
    string Postcode,
    string Country,
    double Lat,
    double Lng,
    string Summary,
    IReadOnlyList<string> Description,
    IReadOnlyList<AdminRoomDto> Rooms);

public sealed class AdminPropertyRequest
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
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Summary { get; set; } = "";
    public List<string> Description { get; set; } = new();
    public List<AdminRoomRequest> Rooms { get; set; } = new();
}
