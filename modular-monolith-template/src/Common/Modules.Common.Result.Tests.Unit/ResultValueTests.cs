using Modules.Common.Domain.Results;

namespace Modules.Common.Result.Tests.Unit;

public class ResultValueTests
{
    [Fact]
    public void Value_ShouldReturnValue_WhenResultIsSuccess()
    {
        // Arrange
        const string expectedValue = "test value";
        Result<string> result = expectedValue;

        // Act
        var actualValue = result.Value;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void Value_ShouldThrowInvalidOperationException_WhenResultIsError()
    {
        // Arrange
        var error = Error.Validation("Test.Error", "Test error description");
        Result<string> result = error;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Equal("The Value property cannot be accessed when Errors property is not empty. Check IsSuccess or IsError before accessing the Value.", exception.Message);
    }

    [Fact]
    public void FirstError_ShouldReturnFirstError_WhenResultHasErrors()
    {
        // Arrange
        var error1 = Error.Validation("Test.Error1", "Test error description 1");
        var error2 = Error.Validation("Test.Error2", "Test error description 2");
        Result<string> result = new[] { error1, error2 };

        // Act
        var firstError = result.FirstError;

        // Assert
        Assert.Equal(error1, firstError);
    }

    [Fact]
    public void FirstError_ShouldThrowInvalidOperationException_WhenResultHasNoErrors()
    {
        // Arrange
        Result<string> result = "test value";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.FirstError);
        Assert.Equal("The FirstError property cannot be accessed when Errors property is empty. Check IsError before accessing FirstError.", exception.Message);
    }

    [Fact]
    public void IsSuccess_ShouldBeTrue_WhenResultCreatedFromValue()
    {
        // Arrange & Act
        Result<string> result = "test value";

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_ShouldBeFalse_WhenResultCreatedFromError()
    {
        // Arrange & Act
        Result<string> result = Error.Validation("Test.Error", "Test error description");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsError_ShouldBeFalse_WhenResultCreatedFromValue()
    {
        // Arrange & Act
        Result<string> result = "test value";

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void IsError_ShouldBeTrue_WhenResultCreatedFromError()
    {
        // Arrange & Act
        Result<string> result = Error.Validation("Test.Error", "Test error description");

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public void Errors_ShouldBeEmpty_WhenResultIsSuccess()
    {
        // Arrange & Act
        Result<string> result = "test value";

        // Assert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Errors_ShouldContainAllErrors_WhenResultIsError()
    {
        // Arrange
        var error1 = Error.Validation("Test.Error1", "Test error description 1");
        var error2 = Error.Validation("Test.Error2", "Test error description 2");
        var errors = new[] { error1, error2 };

        // Act
        Result<string> result = errors;

        // Assert
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(error1, result.Errors);
        Assert.Contains(error2, result.Errors);
    }
}
