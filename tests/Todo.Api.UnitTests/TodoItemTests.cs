using FluentAssertions;

namespace Todo.Api.UnitTests;

public class TodoItemTests
{
    [Fact]
    public void TodoItem_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var todo = new TodoItem { Title = "Test Todo" };

        // Assert
        todo.Id.Should().Be(0);
        todo.Title.Should().Be("Test Todo");
        todo.IsCompleted.Should().BeFalse();
        todo.Priority.Should().Be(TodoPriority.Medium);
    }

    [Fact]
    public void TodoItem_Should_Allow_Setting_Properties()
    {
        // Arrange
        var todo = new TodoItem { Title = "Initial Title" };

        // Act
        todo.Id = 1;
        todo.Title = "Updated Title";
        todo.IsCompleted = true;

        // Assert
        todo.Id.Should().Be(1);
        todo.Title.Should().Be("Updated Title");
        todo.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void TodoItem_Should_Toggle_IsCompleted()
    {
        // Arrange
        var todo = new TodoItem { Title = "Test Todo", IsCompleted = false };

        // Act
        todo.IsCompleted = !todo.IsCompleted;

        // Assert
        todo.IsCompleted.Should().BeTrue();

        // Act
        todo.IsCompleted = !todo.IsCompleted;

        // Assert
        todo.IsCompleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("Buy groceries")]
    [InlineData("Complete project")]
    [InlineData("Call mom")]
    public void TodoItem_Should_Accept_Various_Titles(string title)
    {
        // Arrange & Act
        var todo = new TodoItem { Title = title };

        // Assert
        todo.Title.Should().Be(title);
    }

    [Theory]
    [InlineData(TodoPriority.Low)]
    [InlineData(TodoPriority.Medium)]
    [InlineData(TodoPriority.High)]
    public void TodoItem_Should_Accept_Various_Priorities(TodoPriority priority)
    {
        // Arrange & Act
        var todo = new TodoItem { Title = "Test Todo", Priority = priority };

        // Assert
        todo.Priority.Should().Be(priority);
    }

    [Fact]
    public void TodoItem_Should_Allow_Setting_Priority()
    {
        // Arrange
        var todo = new TodoItem { Title = "Test Todo", Priority = TodoPriority.Low };

        // Act
        todo.Priority = TodoPriority.High;

        // Assert
        todo.Priority.Should().Be(TodoPriority.High);
    }
}
