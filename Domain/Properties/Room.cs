namespace Portico.Domain.Properties;

public class Room
{
    public string PropertyId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public int Size { get; set; }
    public string Beds { get; set; } = "";
    public int Sleeps { get; set; }
    public decimal Price { get; set; }
    public bool Refundable { get; set; }
    public bool Breakfast { get; set; }
    public int Inventory { get; set; }
    public string FeaturesJson { get; set; } = "[]";

    public Property? Property { get; set; }
}
