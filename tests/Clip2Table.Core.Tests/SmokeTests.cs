namespace Clip2Table.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_assembly_loads()
    {
        Assert.Equal("Clip2Table.Core", typeof(Clip2Table.Core.ParsedTable).Assembly.GetName().Name);
    }
}
