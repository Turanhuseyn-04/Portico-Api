namespace Portico.Domain.Properties;

public class PropertyPhoto
{
    public int Id { get; set; }
    public string PropertyId { get; set; } = "";
    public string Src { get; set; } = "";
    public string Alt { get; set; } = "";
    public int Order { get; set; }

    public Property? Property { get; set; }
}
