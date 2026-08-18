namespace Restaurant.Application.Tests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void Application_assembly_is_available()
    {
        Assert.NotNull(typeof(Application.AssemblyMarker).Assembly);
    }
}
