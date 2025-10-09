using Modules.Common.Domain.Results;

namespace Modules.Common.Result.Tests.Unit;

public class SuccessTests
{
    [Fact]
    public void Success_ShouldBeReadOnlyStruct()
    {
        // Arrange
        var type = typeof(Success);

        // Assert
        Assert.True(type.IsValueType);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void Success_DefaultShouldBeEqual()
    {
        // Arrange
        var success1 = default(Success);
        var success2 = default(Success);

        // Act & Assert
        Assert.Equal(success1, success2);
    }

    [Fact]
    public void Success_ResultSuccessShouldBeEqualToDefault()
    {
        // Arrange
        var success1 = Domain.Results.Result.Success;
        var success2 = default(Success);

        // Act & Assert
        Assert.Equal(success1, success2);
    }
}
