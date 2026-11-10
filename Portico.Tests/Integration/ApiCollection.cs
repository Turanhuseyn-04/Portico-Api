namespace Portico.Tests.Integration;

// Program.cs sets Serilog's static bootstrap logger, which only one starting host may freeze at a time,
// so the in-memory API hosts start one after another instead of in parallel.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiCollection
{
    public const string Name = "Portico.Api in memory";
}
