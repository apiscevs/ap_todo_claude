using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Api.IntegrationTests;

public class DatabaseTests : IClassFixture<TodoApiFactory>
{
    private readonly TodoApiFactory _factory;

    public DatabaseTests(TodoApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_Should_Be_Created_And_Migrated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

        // Act
        var canConnect = await db.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task TodoDbContext_Should_Have_Todos_DbSet()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

        // Act
        var todos = db.Todos;

        // Assert
        todos.Should().NotBeNull();
    }

    [Fact]
    public async Task Database_Should_Support_CRUD_Operations()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        // Act - Create
        var todo = new TodoItem { Title = "Database Test" };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        // Assert - Create
        todo.Id.Should().BeGreaterThan(0);

        // Act - Read
        var retrievedTodo = await db.Todos.FindAsync(todo.Id);

        // Assert - Read
        retrievedTodo.Should().NotBeNull();
        retrievedTodo!.Title.Should().Be("Database Test");

        // Act - Update
        retrievedTodo.Title = "Updated Title";
        await db.SaveChangesAsync();
        var updatedTodo = await db.Todos.FindAsync(todo.Id);

        // Assert - Update
        updatedTodo!.Title.Should().Be("Updated Title");

        // Act - Delete
        db.Todos.Remove(updatedTodo);
        await db.SaveChangesAsync();
        var deletedTodo = await db.Todos.FindAsync(todo.Id);

        // Assert - Delete
        deletedTodo.Should().BeNull();
    }

    [Fact]
    public async Task Database_Should_Auto_Increment_Id()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        // Act
        var todo1 = new TodoItem { Title = "First" };
        var todo2 = new TodoItem { Title = "Second" };
        db.Todos.AddRange(todo1, todo2);
        await db.SaveChangesAsync();

        // Assert
        todo1.Id.Should().BeGreaterThan(0);
        todo2.Id.Should().BeGreaterThan(0);
        todo2.Id.Should().BeGreaterThan(todo1.Id);
    }

    [Fact]
    public async Task Database_Should_Handle_Multiple_Todos()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        // Act
        var todos = Enumerable.Range(1, 10)
            .Select(i => new TodoItem { Title = $"Todo {i}" })
            .ToList();

        db.Todos.AddRange(todos);
        await db.SaveChangesAsync();

        var count = await db.Todos.CountAsync();

        // Assert
        count.Should().Be(10);
    }

    [Fact]
    public async Task Database_Should_Support_Querying_By_IsCompleted()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        var completedTodo = new TodoItem { Title = "Completed", IsCompleted = true };
        var incompleteTodo = new TodoItem { Title = "Incomplete", IsCompleted = false };
        db.Todos.AddRange(completedTodo, incompleteTodo);
        await db.SaveChangesAsync();

        // Act
        var completed = await db.Todos.Where(t => t.IsCompleted).ToListAsync();
        var incomplete = await db.Todos.Where(t => !t.IsCompleted).ToListAsync();

        // Assert
        completed.Should().HaveCount(1);
        completed[0].Title.Should().Be("Completed");
        incomplete.Should().HaveCount(1);
        incomplete[0].Title.Should().Be("Incomplete");
    }

    [Fact]
    public async Task Database_Should_Support_Ordering()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        var titles = new[] { "Charlie", "Alice", "Bob" };
        foreach (var title in titles)
        {
            db.Todos.Add(new TodoItem { Title = title });
            await db.SaveChangesAsync();
        }

        // Act
        var orderedByTitle = await db.Todos.OrderBy(t => t.Title).ToListAsync();
        var orderedById = await db.Todos.OrderBy(t => t.Id).ToListAsync();

        // Assert
        orderedByTitle.Select(t => t.Title).Should().ContainInOrder("Alice", "Bob", "Charlie");
        orderedById.Select(t => t.Title).Should().ContainInOrder("Charlie", "Alice", "Bob");
    }

    [Fact]
    public async Task Database_Should_Handle_Concurrent_Operations()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        // Act
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            using var taskScope = _factory.Services.CreateScope();
            var taskDb = taskScope.ServiceProvider.GetRequiredService<TodoDbContext>();
            var todo = new TodoItem { Title = $"Concurrent {i}" };
            taskDb.Todos.Add(todo);
            await taskDb.SaveChangesAsync();
            return todo.Id;
        });

        var ids = await Task.WhenAll(tasks);

        // Assert
        ids.Should().HaveCount(5);
        ids.Should().OnlyHaveUniqueItems();

        var count = await db.Todos.CountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task Database_Should_Enforce_Required_Title()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        // This test verifies that the database schema enforces NOT NULL on Title
        // by attempting to insert a record with null Title via raw SQL
        // (C# model validation prevents this at compile time due to 'required' keyword)

        // Act & Assert
        var act = async () =>
        {
            await db.Database.ExecuteSqlRawAsync("INSERT INTO \"Todos\" (\"Title\", \"IsCompleted\") VALUES (NULL, false)");
        };

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Database_Should_Default_IsCompleted_To_False()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await ClearDatabase();

        // Act
        var todo = new TodoItem { Title = "Default Test" };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        // Clear context to force fresh read from database
        db.ChangeTracker.Clear();
        var retrievedTodo = await db.Todos.FindAsync(todo.Id);

        // Assert
        retrievedTodo!.IsCompleted.Should().BeFalse();
    }

    private async Task ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        db.Todos.RemoveRange(db.Todos);
        await db.SaveChangesAsync();
    }
}
