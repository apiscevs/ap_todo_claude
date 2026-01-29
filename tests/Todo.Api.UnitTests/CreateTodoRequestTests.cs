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

    [Fact]
    public void CreateTodoRequest_Should_Default_Priority_To_Medium()
    {
        // Arrange & Act
        var request = new CreateTodoRequest("Test Todo");

        // Assert
        request.Priority.Should().Be(TodoPriority.Medium);
    }

    [Theory]
    [InlineData(TodoPriority.Low)]
    [InlineData(TodoPriority.Medium)]
    [InlineData(TodoPriority.High)]
    public void CreateTodoRequest_Should_Accept_Various_Priorities(TodoPriority priority)
    {
        // Arrange & Act
        var request = new CreateTodoRequest("Test Todo", priority);

        // Assert
        request.Priority.Should().Be(priority);
    }

    [Fact]
    public void CreateTodoRequest_Should_Support_Record_Equality_With_Priority()
    {
        // Arrange
        var request1 = new CreateTodoRequest("Test", TodoPriority.High);
        var request2 = new CreateTodoRequest("Test", TodoPriority.High);
        var request3 = new CreateTodoRequest("Test", TodoPriority.Low);

        // Assert - Records have value equality
        request1.Should().Be(request2);
        request1.Should().NotBe(request3);
    }
}
