namespace Portico.Domain.Properties;

public class Review
{
    public int Id { get; set; }
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

    public Property? Property { get; set; }
}
