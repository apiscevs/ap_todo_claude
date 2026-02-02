using HotChocolate;
using HotChocolate.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Todo.Api.GraphQL;

public class Query
{
    [UseDbContext(typeof(TodoDbContext))]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TodoItem> GetTodos(
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        ClaimsPrincipal user,
        [Service] UserManager<ApplicationUser> userManager)
    {
        var userId = userManager.GetUserId(user);
        if (userId is null)
            throw new GraphQLException("Unauthorized");

        return db.Todos
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Id);
    }

    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem?> GetTodoById(
        int id,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        ClaimsPrincipal user,
        [Service] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var userId = userManager.GetUserId(user);
        if (userId is null)
            throw new GraphQLException("Unauthorized");

        return await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
    }
}
