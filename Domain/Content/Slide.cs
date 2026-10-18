namespace Portico.Domain.Content;

public class Slide
{
    public int Id { get; set; }
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Image { get; set; } = "";
    public int Stays { get; set; }
    public int Order { get; set; }
}
