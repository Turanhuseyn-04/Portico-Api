namespace Portico.Domain.Platform;

// A change made by staff: who, when, what request, and how it ended.
public class AuditEntry
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public string ActorEmail { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public int StatusCode { get; set; }
    public string? Detail { get; set; }
}
