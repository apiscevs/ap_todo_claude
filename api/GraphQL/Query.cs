using HotChocolate;
using HotChocolate.Data;

namespace Todo.Api.GraphQL;

public class Query
{
    [UseDbContext(typeof(TodoDbContext))]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TodoItem> GetTodos([Service(ServiceKind.Synchronized)] TodoDbContext db)
        => db.Todos.OrderBy(t => t.Id);

    [UseDbContext(typeof(TodoDbContext))]
    public async Task<TodoItem?> GetTodoById(
        int id,
        [Service(ServiceKind.Synchronized)] TodoDbContext db,
        CancellationToken ct)
        => await db.Todos.FindAsync([id], ct);
}
