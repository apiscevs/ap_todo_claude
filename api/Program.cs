using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<TodoDbContext>("tododb");
builder.AddRedisOutputCache("cache");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    var retries = 5;
    for (var i = 0; i < retries; i++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception) when (i < retries - 1)
        {
            await Task.Delay(2000);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseCors();
app.UseOutputCache();

app.MapGet("/api/todos", async (TodoDbContext db) =>
    await db.Todos.OrderBy(t => t.Id).ToListAsync())
    .CacheOutput(p => p.Tag("todos"));

app.MapPost("/api/todos", async (CreateTodoRequest request, TodoDbContext db, IOutputCacheStore cache, CancellationToken ct) =>
{
    var todo = new TodoItem { Title = request.Title };
    db.Todos.Add(todo);
    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.MapPut("/api/todos/{id}/toggle", async (int id, TodoDbContext db, IOutputCacheStore cache, CancellationToken ct) =>
{
    var todo = await db.Todos.FindAsync([id], ct);
    if (todo is null)
        return Results.NotFound();

    todo.IsCompleted = !todo.IsCompleted;
    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.Ok(todo);
});

app.MapDelete("/api/todos/{id}", async (int id, TodoDbContext db, IOutputCacheStore cache, CancellationToken ct) =>
{
    var todo = await db.Todos.FindAsync([id], ct);
    if (todo is null)
        return Results.NotFound();

    db.Todos.Remove(todo);
    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.NoContent();
});

app.Run();

record CreateTodoRequest(string Title);
