using HotChocolate;
using Microsoft.AspNetCore.OutputCaching;

namespace Todo.Api.GraphQL;

public class Mutation
{
    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem> CreateTodo(
        CreateTodoInput input,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var todo = new TodoItem { Title = input.Title };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("todos", ct);
        return todo;
    }

    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem> ToggleTodo(
        int id,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null)
            throw new GraphQLException("Todo not found");

        todo.IsCompleted = !todo.IsCompleted;
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("todos", ct);
        return todo;
    }

    [UseDbContext(typeof(TodoDbContext))]
    public async Task<bool> DeleteTodo(
        int id,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null)
            throw new GraphQLException("Todo not found");

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("todos", ct);
        return true;
    }
}

public record CreateTodoInput(string Title);
