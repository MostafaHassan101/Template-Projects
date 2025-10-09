using Modules.Common.Domain.Results;

namespace Modules.Common.Result.Tests.Unit;

public class ErrorTypeTests
{
    [Fact]
    public void ErrorType_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ErrorType.Failure);
        Assert.Equal(1, (int)ErrorType.Unexpected);
        Assert.Equal(2, (int)ErrorType.Validation);
        Assert.Equal(3, (int)ErrorType.Conflict);
        Assert.Equal(4, (int)ErrorType.NotFound);
        Assert.Equal(5, (int)ErrorType.Unauthorized);
        Assert.Equal(6, (int)ErrorType.Forbidden);
        Assert.Equal(7, (int)ErrorType.Custom);
    }

    [Fact]
    public void ErrorType_ShouldHaveExpectedCount()
    {
        // Act
        var values = Enum.GetValues<ErrorType>();

        // Assert
        Assert.Equal(8, values.Length);
    }

    [Fact]
    public void ErrorType_ShouldBeUsedInErrorCreation()
    {
        // Arrange & Act
        var failureError = Error.Failure("Test.Code", "Test description");
        var unexpectedError = Error.Unexpected("Test.Code", "Test description");
        var validationError = Error.Validation("Test.Code", "Test description");
        var conflictError = Error.Conflict("Test.Code", "Test description");
        var notFoundError = Error.NotFound("Test.Code", "Test description");
        var unauthorizedError = Error.Unauthorized("Test.Code", "Test description");
        var forbiddenError = Error.Forbidden("Test.Code", "Test description");
        var customError = Error.Custom(100, "Test.Code", "Test description");

        // Assert
        Assert.Equal(ErrorType.Failure, failureError.Type);
        Assert.Equal(ErrorType.Unexpected, unexpectedError.Type);
        Assert.Equal(ErrorType.Validation, validationError.Type);
        Assert.Equal(ErrorType.Conflict, conflictError.Type);
        Assert.Equal(ErrorType.NotFound, notFoundError.Type);
        Assert.Equal(ErrorType.Unauthorized, unauthorizedError.Type);
        Assert.Equal(ErrorType.Forbidden, forbiddenError.Type);
        Assert.Equal(ErrorType.Custom, customError.Type);
    }
}
