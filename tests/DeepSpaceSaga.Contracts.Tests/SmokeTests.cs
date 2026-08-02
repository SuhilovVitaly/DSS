using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class SmokeTests
{
    [Fact]
    public void Command_is_instantiable()
    {
        var command = new Command();
        Assert.NotNull(command);
    }

    [Fact]
    public void AuthoritativeSnapshot_is_instantiable()
    {
        var snapshot = new AuthoritativeSnapshot();
        Assert.NotNull(snapshot);
    }

    [Fact]
    public void IGameSessionConnection_is_defined()
    {
        var type = typeof(IGameSessionConnection);
        Assert.True(type.IsInterface);
    }
}
