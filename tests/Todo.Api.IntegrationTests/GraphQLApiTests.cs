using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Api.IntegrationTests;

public class GraphQLApiTests : IClassFixture<TodoApiFactory>, IAsyncLifetime
{
    private readonly TodoApiFactory _factory;
    private HttpClient _client = null!;
    private string _userId = string.Empty;

    public GraphQLApiTests(TodoApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var session = await _factory.CreateAuthenticatedClientAsync();
        _client = session.Client;
        _userId = session.UserId;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Query_GetTodos_Should_Return_Empty_List_Initially()
    {
        // Arrange
        await ClearDatabase();
        var query = @"
            query {
                todos {
                    id
                    title
                    isCompleted
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(query);
        var todos = response.RootElement.GetProperty("data").GetProperty("todos");

        // Assert
        todos.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Query_GetTodos_Should_Return_All_Todos()
    {
        // Arrange
        await ClearDatabase();
        await SeedTodo("First Todo", TodoPriority.High);
        await SeedTodo("Second Todo", TodoPriority.Low);

        var query = @"
            query {
                todos {
                    id
                    title
                    isCompleted
                    priority
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(query);
        var todos = response.RootElement.GetProperty("data").GetProperty("todos");

        // Assert
        todos.GetArrayLength().Should().Be(2);
        todos[0].GetProperty("title").GetString().Should().Be("First Todo");
        todos[0].GetProperty("priority").GetString().Should().Be("HIGH");
        todos[1].GetProperty("title").GetString().Should().Be("Second Todo");
        todos[1].GetProperty("priority").GetString().Should().Be("LOW");
    }

    [Fact]
    public async Task Query_GetTodoById_Should_Return_Specific_Todo()
    {
        // Arrange
        await ClearDatabase();
        var todoId = await SeedTodo("Specific Todo", TodoPriority.Medium);

        var query = $@"
            query {{
                todoById(id: {todoId}) {{
                    id
                    title
                    isCompleted
                    priority
                }}
            }}";

        // Act
        var response = await ExecuteGraphQLQuery(query);
        var todo = response.RootElement.GetProperty("data").GetProperty("todoById");

        // Assert
        todo.GetProperty("id").GetInt32().Should().Be(todoId);
        todo.GetProperty("title").GetString().Should().Be("Specific Todo");
        todo.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
        todo.GetProperty("priority").GetString().Should().Be("MEDIUM");
    }

    [Fact]
    public async Task Query_GetTodoById_Should_Return_Null_For_NonExistent_Todo()
    {
        // Arrange
        await ClearDatabase();
        var query = @"
            query {
                todoById(id: 999) {
                    id
                    title
                    isCompleted
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(query);
        var todo = response.RootElement.GetProperty("data").GetProperty("todoById");

        // Assert
        todo.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Mutation_CreateTodo_Should_Create_New_Todo()
    {
        // Arrange
        await ClearDatabase();
        var mutation = @"
            mutation {
                createTodo(input: { title: ""GraphQL Todo"" }) {
                    id
                    title
                    isCompleted
                    priority
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(mutation);
        var todo = response.RootElement.GetProperty("data").GetProperty("createTodo");

        // Assert
        todo.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        todo.GetProperty("title").GetString().Should().Be("GraphQL Todo");
        todo.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
        todo.GetProperty("priority").GetString().Should().Be("MEDIUM");
    }

    [Fact]
    public async Task Mutation_ToggleTodo_Should_Toggle_Completion_Status()
    {
        // Arrange
        await ClearDatabase();
        var todoId = await SeedTodo("Toggle Me", TodoPriority.High);

        var mutation = $@"
            mutation {{
                toggleTodo(id: {todoId}) {{
                    id
                    title
                    isCompleted
                }}
            }}";

        // Act
        var response = await ExecuteGraphQLQuery(mutation);
        var todo = response.RootElement.GetProperty("data").GetProperty("toggleTodo");

        // Assert
        todo.GetProperty("id").GetInt32().Should().Be(todoId);
        todo.GetProperty("isCompleted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Mutation_ToggleTodo_Should_Throw_Error_For_NonExistent_Todo()
    {
        // Arrange
        await ClearDatabase();
        var mutation = @"
            mutation {
                toggleTodo(id: 999) {
                    id
                    title
                    isCompleted
                }
            }";

        // Act
        var response = await ExecuteGraphQLQueryAllowErrors(mutation);

        // Assert
        response.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
        errors[0].GetProperty("message").GetString().Should().Contain("Todo not found");
    }

    [Fact]
    public async Task Mutation_DeleteTodo_Should_Delete_Todo()
    {
        // Arrange
        await ClearDatabase();
        var todoId = await SeedTodo("Delete Me", TodoPriority.Low);

        var mutation = $@"
            mutation {{
                deleteTodo(id: {todoId})
            }}";

        // Act
        var response = await ExecuteGraphQLQuery(mutation);
        var result = response.RootElement.GetProperty("data").GetProperty("deleteTodo");

        // Assert
        result.GetBoolean().Should().BeTrue();

        // Verify deletion
        var verifyQuery = @"
            query {
                todos {
                    id
                }
            }";
        var verifyResponse = await ExecuteGraphQLQuery(verifyQuery);
        var todos = verifyResponse.RootElement.GetProperty("data").GetProperty("todos");
        todos.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Mutation_DeleteTodo_Should_Throw_Error_For_NonExistent_Todo()
    {
        // Arrange
        await ClearDatabase();
        var mutation = @"
            mutation {
                deleteTodo(id: 999)
            }";

        // Act
        var response = await ExecuteGraphQLQueryAllowErrors(mutation);

        // Assert
        response.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
        errors[0].GetProperty("message").GetString().Should().Contain("Todo not found");
    }

    [Fact]
    public async Task Query_WithProjection_Should_Return_Only_Requested_Fields()
    {
        // Arrange
        await ClearDatabase();
        await SeedTodo("Test Todo", TodoPriority.Medium);

        var query = @"
            query {
                todos {
                    title
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(query);
        var todos = response.RootElement.GetProperty("data").GetProperty("todos");

        // Assert
        todos.GetArrayLength().Should().Be(1);
        todos[0].TryGetProperty("title", out _).Should().BeTrue();
        todos[0].TryGetProperty("id", out _).Should().BeFalse();
        todos[0].TryGetProperty("isCompleted", out _).Should().BeFalse();
        todos[0].TryGetProperty("priority", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CompleteWorkflow_Should_Work_End_To_End()
    {
        // Arrange
        await ClearDatabase();

        // Act & Assert - Create
        var createMutation = @"
            mutation {
                createTodo(input: { title: ""GraphQL Workflow Test"" }) {
                    id
                    title
                    isCompleted
                }
            }";
        var createResponse = await ExecuteGraphQLQuery(createMutation);
        var createdTodo = createResponse.RootElement.GetProperty("data").GetProperty("createTodo");
        var todoId = createdTodo.GetProperty("id").GetInt32();

        // Act & Assert - Query
        var query = @"
            query {
                todos {
                    id
                    title
                }
            }";
        var queryResponse = await ExecuteGraphQLQuery(query);
        var todos = queryResponse.RootElement.GetProperty("data").GetProperty("todos");
        todos.GetArrayLength().Should().Be(1);

        // Act & Assert - Toggle
        var toggleMutation = $@"
            mutation {{
                toggleTodo(id: {todoId}) {{
                    isCompleted
                }}
            }}";
        var toggleResponse = await ExecuteGraphQLQuery(toggleMutation);
        var toggledTodo = toggleResponse.RootElement.GetProperty("data").GetProperty("toggleTodo");
        toggledTodo.GetProperty("isCompleted").GetBoolean().Should().BeTrue();

        // Act & Assert - Delete
        var deleteMutation = $@"
            mutation {{
                deleteTodo(id: {todoId})
            }}";
        var deleteResponse = await ExecuteGraphQLQuery(deleteMutation);
        var deleteResult = deleteResponse.RootElement.GetProperty("data").GetProperty("deleteTodo");
        deleteResult.GetBoolean().Should().BeTrue();

        // Act & Assert - Verify Empty
        var finalQueryResponse = await ExecuteGraphQLQuery(query);
        var finalTodos = finalQueryResponse.RootElement.GetProperty("data").GetProperty("todos");
        finalTodos.GetArrayLength().Should().Be(0);
    }

    private async Task<JsonDocument> ExecuteGraphQLQuery(string query)
    {
        var request = new
        {
            query = query
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/graphql", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(responseContent);
    }

    private async Task<JsonDocument> ExecuteGraphQLQueryAllowErrors(string query)
    {
        var request = new
        {
            query = query
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/graphql", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(responseContent);
    }

    [Theory]
    [InlineData("LOW")]
    [InlineData("MEDIUM")]
    [InlineData("HIGH")]
    public async Task Mutation_CreateTodo_Should_Accept_All_Priority_Levels(string priority)
    {
        // Arrange
        await ClearDatabase();
        var mutation = $@"
            mutation {{
                createTodo(input: {{ title: ""Priority Test"", priority: {priority} }}) {{
                    id
                    title
                    priority
                }}
            }}";

        // Act
        var response = await ExecuteGraphQLQuery(mutation);
        var todo = response.RootElement.GetProperty("data").GetProperty("createTodo");

        // Assert
        todo.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        todo.GetProperty("priority").GetString().Should().Be(priority);
    }

    [Fact]
    public async Task Mutation_CreateTodo_Without_Priority_Should_Default_To_MEDIUM()
    {
        // Arrange
        await ClearDatabase();
        var mutation = @"
            mutation {
                createTodo(input: { title: ""Default Priority Test"" }) {
                    id
                    title
                    priority
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(mutation);
        var todo = response.RootElement.GetProperty("data").GetProperty("createTodo");

        // Assert
        todo.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        todo.GetProperty("priority").GetString().Should().Be("MEDIUM");
    }

    [Fact]
    public async Task Mutation_CreateTodo_With_Explicit_Priority_Should_Use_Specified_Priority()
    {
        // Arrange
        await ClearDatabase();
        var mutation = @"
            mutation {
                createTodo(input: { title: ""High Priority Task"", priority: HIGH }) {
                    id
                    title
                    priority
                }
            }";

        // Act
        var response = await ExecuteGraphQLQuery(mutation);
        var todo = response.RootElement.GetProperty("data").GetProperty("createTodo");

        // Assert
        todo.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        todo.GetProperty("priority").GetString().Should().Be("HIGH");
    }

    private async Task<int> SeedTodo(string title, TodoPriority priority = TodoPriority.Medium)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var todo = new TodoItem { Title = title, Priority = priority, UserId = _userId };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return todo.Id;
    }

    private async Task ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var todos = db.Todos.Where(t => t.UserId == _userId);
        db.Todos.RemoveRange(todos);
        await db.SaveChangesAsync();
    }
}
