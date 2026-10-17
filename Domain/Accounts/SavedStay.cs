namespace Portico.Domain.Accounts;

public class SavedStay
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string PropertyId { get; set; } = "";

    public Account? Account { get; set; }
    public Properties.Property? Property { get; set; }
}
