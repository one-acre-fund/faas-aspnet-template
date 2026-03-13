using OpenFaaS.Shared;

namespace Unit;

public class SharedInfoTests
{
    [Fact]
    public void Version_ShouldReturnExpectedValue()
    {
        var version = SharedInfo.Version;
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version);
    }
}
