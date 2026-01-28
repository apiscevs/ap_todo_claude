using FluentAssertions;

namespace Todo.Api.UnitTests;

public class CreateTodoRequestTests
{
    [Fact]
    public void CreateTodoRequest_Should_Initialize_With_Title()
    {
        // Arrange
        const string title = "Test Todo";

        // Act
        var request = new CreateTodoRequest(title);

        // Assert
        request.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("Buy groceries")]
    [InlineData("Complete project")]
    [InlineData("Call mom")]
    public void CreateTodoRequest_Should_Accept_Various_Titles(string title)
    {
        // Arrange & Act
        var request = new CreateTodoRequest(title);

        // Assert
        request.Title.Should().Be(title);
    }

    [Fact]
    public void CreateTodoRequest_Should_Be_Record_Type()
    {
        // Arrange
        var request1 = new CreateTodoRequest("Test");
        var request2 = new CreateTodoRequest("Test");
        var request3 = new CreateTodoRequest("Different");

        // Assert - Records have value equality
        request1.Should().Be(request2);
        request1.Should().NotBe(request3);
    }
}
