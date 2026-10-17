namespace Portico.Domain.Accounts;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";

    public ICollection<SavedStay> SavedStays { get; set; } = new List<SavedStay>();
}
