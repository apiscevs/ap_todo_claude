using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

var todos = new ConcurrentDictionary<int, TodoItem>();
var nextId = 0;

app.MapGet("/api/todos", () => todos.Values.OrderBy(t => t.Id));

app.MapPost("/api/todos", (CreateTodoRequest request) =>
{
    var id = Interlocked.Increment(ref nextId);
    var todo = new TodoItem(id, request.Title, false);
    todos[id] = todo;
    return Results.Created($"/api/todos/{id}", todo);
});

app.MapPut("/api/todos/{id}/toggle", (int id) =>
{
    if (!todos.TryGetValue(id, out var todo))
        return Results.NotFound();

    var toggled = todo with { IsCompleted = !todo.IsCompleted };
    todos[id] = toggled;
    return Results.Ok(toggled);
});

app.MapDelete("/api/todos/{id}", (int id) =>
{
    if (!todos.TryRemove(id, out _))
        return Results.NotFound();

    return Results.NoContent();
});

app.Run();

record TodoItem(int Id, string Title, bool IsCompleted);
record CreateTodoRequest(string Title);
