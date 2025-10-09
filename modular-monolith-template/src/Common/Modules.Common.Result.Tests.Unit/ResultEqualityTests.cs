using Modules.Common.Domain.Results;

namespace Modules.Common.Result.Tests.Unit;

public class ResultEqualityTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothResultsContainSameValue()
    {
        // Arrange
        Result<string> result1 = "test value";
        Result<string> result2 = "test value";

        // Act & Assert
        Assert.Equal(result1.IsSuccess, result2.IsSuccess);
        Assert.Equivalent(result1.Value, result2.Value);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenResultsContainDifferentValues()
    {
        // Arrange
        Result<string> result1 = "test value 1";
        Result<string> result2 = "test value 2";

        // Act & Assert
        Assert.NotEqual(result1.Value, result2.Value);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothResultsContainSameError()
    {
        // Arrange
        var error = Error.Validation("Test.Error", "Test error description");
        Result<string> result1 = error;
        Result<string> result2 = error;

        // Act & Assert
        Assert.Equal(result1.IsError, result2.IsError);
        Assert.Equivalent(result1.Errors, result2.Errors);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenResultsContainDifferentErrors()
    {
        // Arrange
        Result<string> result1 = Error.Validation("Test.Error1", "Test error description 1");
        Result<string> result2 = Error.Validation("Test.Error2", "Test error description 2");

        // Act & Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenOneResultHasValueAndOtherHasError()
    {
        // Arrange
        Result<string> result1 = "test value";
        Result<string> result2 = Error.Validation("Test.Error", "Test error description");

        // Act & Assert
        Assert.NotEqual(result1, result2);
    }
}
