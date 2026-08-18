namespace Restaurant.Domain.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void Domain_assembly_is_available()
    {
        Assert.NotNull(typeof(Domain.AssemblyMarker).Assembly);
    }
}
