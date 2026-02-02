using HotChocolate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Todo.Api.GraphQL;

public class Mutation
{
    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem> CreateTodo(
        CreateTodoInput input,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        ClaimsPrincipal user,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var userId = userManager.GetUserId(user);
        if (userId is null)
            throw new GraphQLException("Unauthorized");

        var (startAtUtc, endAtUtc) = TodoSchedule.Normalize(input.StartAtUtc, input.EndAtUtc);
        if (!TodoSchedule.TryValidate(startAtUtc, endAtUtc, out var error))
            throw new GraphQLException(error);

        var todo = new TodoItem
        {
            Title = input.Title,
            Description = input.Description ?? string.Empty,
            Priority = input.Priority,
            StartAtUtc = startAtUtc,
            EndAtUtc = endAtUtc,
            UserId = userId
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("todos", ct);
        return todo;
    }

    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem> UpdateTodo(
        int id,
        UpdateTodoInput input,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        ClaimsPrincipal user,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var userId = userManager.GetUserId(user);
        if (userId is null)
            throw new GraphQLException("Unauthorized");

        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (todo is null)
            throw new GraphQLException("Todo not found");

        var (startAtUtc, endAtUtc) = TodoSchedule.Normalize(input.StartAtUtc, input.EndAtUtc);
        if (!TodoSchedule.TryValidate(startAtUtc, endAtUtc, out var error))
            throw new GraphQLException(error);

        todo.Title = input.Title;
        todo.Description = input.Description ?? string.Empty;
        todo.IsCompleted = input.IsCompleted;
        todo.Priority = input.Priority;
        todo.StartAtUtc = startAtUtc;
        todo.EndAtUtc = endAtUtc;

        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("todos", ct);
        return todo;
    }

    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem> ToggleTodo(
        int id,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        ClaimsPrincipal user,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var userId = userManager.GetUserId(user);
        if (userId is null)
            throw new GraphQLException("Unauthorized");

        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
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
        ClaimsPrincipal user,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IOutputCacheStore cache,
        CancellationToken ct)
    {
        var userId = userManager.GetUserId(user);
        if (userId is null)
            throw new GraphQLException("Unauthorized");

        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (todo is null)
            throw new GraphQLException("Todo not found");

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("todos", ct);
        return true;
    }
}

public record CreateTodoInput(
    string Title,
    string? Description = null,
    TodoPriority Priority = TodoPriority.Medium,
    DateTime? StartAtUtc = null,
    DateTime? EndAtUtc = null);

public record UpdateTodoInput(
    string Title,
    bool IsCompleted,
    TodoPriority Priority,
    string? Description,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc);
