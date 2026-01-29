using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Api.IntegrationTests;

public class RestApiTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client;
    private readonly TodoApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RestApiTests(TodoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTodos_Should_Return_Empty_List_Initially()
    {
        // Arrange
        await ClearDatabase();

        // Act
        var response = await _client.GetAsync("/api/todos");
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        todos.Should().NotBeNull();
        todos.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTodo_Should_Create_New_Todo()
    {
        // Arrange
        await ClearDatabase();
        var request = new { Title = "Test Todo" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        todo.Should().NotBeNull();
        todo!.Id.Should().BeGreaterThan(0);
        todo.Title.Should().Be("Test Todo");
        todo.IsCompleted.Should().BeFalse();
        todo.Priority.Should().Be(TodoPriority.Medium);
    }

    [Fact]
    public async Task CreateTodo_Should_Add_Todo_To_List()
    {
        // Arrange
        await ClearDatabase();
        var request = new { Title = "New Todo" };

        // Act
        await _client.PostAsJsonAsync("/api/todos", request);
        var response = await _client.GetAsync("/api/todos");
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);

        // Assert
        todos.Should().NotBeNull();
        todos.Should().HaveCount(1);
        todos![0].Title.Should().Be("New Todo");
    }

    [Fact]
    public async Task CreateMultipleTodos_Should_Return_Ordered_List()
    {
        // Arrange
        await ClearDatabase();
        var titles = new[] { "First", "Second", "Third" };

        // Act
        foreach (var title in titles)
        {
            await _client.PostAsJsonAsync("/api/todos", new { Title = title });
        }

        var response = await _client.GetAsync("/api/todos");
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);

        // Assert
        todos.Should().NotBeNull();
        todos.Should().HaveCount(3);
        todos!.Select(t => t.Title).Should().ContainInOrder(titles);
    }

    [Fact]
    public async Task ToggleTodo_Should_Toggle_Completion_Status()
    {
        // Arrange
        await ClearDatabase();
        var createResponse = await _client.PostAsJsonAsync("/api/todos", new { Title = "Test Todo" });
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Act
        var toggleResponse = await _client.PutAsync($"/api/todos/{createdTodo!.Id}/toggle", null);
        var toggledTodo = await toggleResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Assert
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        toggledTodo.Should().NotBeNull();
        toggledTodo!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleTodo_Should_Toggle_Back_To_Incomplete()
    {
        // Arrange
        await ClearDatabase();
        var createResponse = await _client.PostAsJsonAsync("/api/todos", new { Title = "Test Todo" });
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Act - Toggle twice
        await _client.PutAsync($"/api/todos/{createdTodo!.Id}/toggle", null);
        var secondToggleResponse = await _client.PutAsync($"/api/todos/{createdTodo.Id}/toggle", null);
        var finalTodo = await secondToggleResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Assert
        secondToggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        finalTodo.Should().NotBeNull();
        finalTodo!.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleTodo_Should_Return_NotFound_For_NonExistent_Todo()
    {
        // Arrange
        await ClearDatabase();
        const int nonExistentId = 999;

        // Act
        var response = await _client.PutAsync($"/api/todos/{nonExistentId}/toggle", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_Should_Remove_Todo()
    {
        // Arrange
        await ClearDatabase();
        var createResponse = await _client.PostAsJsonAsync("/api/todos", new { Title = "Todo to Delete" });
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdTodo!.Id}");
        var getResponse = await _client.GetAsync("/api/todos");
        var todos = await getResponse.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        todos.Should().NotBeNull();
        todos.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTodo_Should_Return_NotFound_For_NonExistent_Todo()
    {
        // Arrange
        await ClearDatabase();
        const int nonExistentId = 999;

        // Act
        var response = await _client.DeleteAsync($"/api/todos/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_Should_Only_Delete_Specified_Todo()
    {
        // Arrange
        await ClearDatabase();
        var todo1Response = await _client.PostAsJsonAsync("/api/todos", new { Title = "Keep This" });
        var todo1 = await todo1Response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);
        var todo2Response = await _client.PostAsJsonAsync("/api/todos", new { Title = "Delete This" });
        var todo2 = await todo2Response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Act
        await _client.DeleteAsync($"/api/todos/{todo2!.Id}");
        var getResponse = await _client.GetAsync("/api/todos");
        var todos = await getResponse.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);

        // Assert
        todos.Should().NotBeNull();
        todos.Should().HaveCount(1);
        todos![0].Id.Should().Be(todo1!.Id);
        todos[0].Title.Should().Be("Keep This");
    }

    [Fact]
    public async Task CompleteWorkflow_Should_Work_End_To_End()
    {
        // Arrange
        await ClearDatabase();

        // Act & Assert - Create
        var createResponse = await _client.PostAsJsonAsync("/api/todos", new { Title = "Complete Workflow Test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await createResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);
        todo.Should().NotBeNull();

        // Act & Assert - Get List
        var listResponse = await _client.GetAsync("/api/todos");
        var todos = await listResponse.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);
        todos.Should().HaveCount(1);

        // Act & Assert - Toggle
        var toggleResponse = await _client.PutAsync($"/api/todos/{todo!.Id}/toggle", null);
        var toggledTodo = await toggleResponse.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);
        toggledTodo!.IsCompleted.Should().BeTrue();

        // Act & Assert - Delete
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{todo.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act & Assert - Verify Empty
        var finalListResponse = await _client.GetAsync("/api/todos");
        var finalTodos = await finalListResponse.Content.ReadFromJsonAsync<List<TodoItem>>(JsonOptions);
        finalTodos.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public async Task CreateTodo_Should_Accept_All_Priority_Levels(string priority)
    {
        // Arrange
        await ClearDatabase();
        var request = new { Title = "Priority Test", Priority = priority };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        todo.Should().NotBeNull();
        todo!.Priority.ToString().Should().Be(priority);
    }

    [Fact]
    public async Task CreateTodo_Without_Priority_Should_Default_To_Medium()
    {
        // Arrange
        await ClearDatabase();
        var request = new { Title = "Default Priority Test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        todo.Should().NotBeNull();
        todo!.Priority.Should().Be(TodoPriority.Medium);
    }

    [Fact]
    public async Task CreateTodo_With_Explicit_Priority_Should_Use_Specified_Priority()
    {
        // Arrange
        await ClearDatabase();
        var request = new { Title = "High Priority Task", Priority = "High" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        todo.Should().NotBeNull();
        todo!.Priority.Should().Be(TodoPriority.High);
    }

    private async Task ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        db.Todos.RemoveRange(db.Todos);
        await db.SaveChangesAsync();
    }
}
