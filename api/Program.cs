using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Todo.Api.GraphQL;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var connectionString = builder.Configuration.GetConnectionString("tododb");
builder.Services.AddDbContextPool<TodoDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddPooledDbContextFactory<TodoDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.EnrichNpgsqlDbContext<TodoDbContext>();
builder.AddRedisOutputCache("cache");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddErrorFilter<ErrorFilter>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    // TODO: Upgrade HotChocolate to a version compatible with EF Core 10 pooled DbContext integration,
    // so we can switch to DbContextKind.Pooled + ServiceKind.Pooled without ObjectPool resolution errors.
    .RegisterDbContext<TodoDbContext>(DbContextKind.Synchronized);

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
    var (startAtUtc, endAtUtc) = TodoSchedule.Normalize(request.StartAtUtc, request.EndAtUtc);
    if (!TodoSchedule.TryValidate(startAtUtc, endAtUtc, out var error))
        return Results.BadRequest(new { error });

    var todo = new TodoItem
    {
        Title = request.Title,
        Description = request.Description ?? string.Empty,
        Priority = request.Priority,
        StartAtUtc = startAtUtc,
        EndAtUtc = endAtUtc
    };
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

app.MapPut("/api/todos/{id}", async (int id, UpdateTodoRequest request, TodoDbContext db, IOutputCacheStore cache, CancellationToken ct) =>
{
    var todo = await db.Todos.FindAsync([id], ct);
    if (todo is null)
        return Results.NotFound();

    var (startAtUtc, endAtUtc) = TodoSchedule.Normalize(request.StartAtUtc, request.EndAtUtc);
    if (!TodoSchedule.TryValidate(startAtUtc, endAtUtc, out var error))
        return Results.BadRequest(new { error });

    todo.Title = request.Title;
    todo.Description = request.Description;
    todo.IsCompleted = request.IsCompleted;
    todo.Priority = request.Priority;
    todo.StartAtUtc = startAtUtc;
    todo.EndAtUtc = endAtUtc;

    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.Ok(todo);
});

app.MapGraphQL("/graphql");

app.Run();

public record CreateTodoRequest(
    string Title,
    TodoPriority Priority = TodoPriority.Medium,
    string? Description = null,
    DateTime? StartAtUtc = null,
    DateTime? EndAtUtc = null);

public record UpdateTodoRequest(
    string Title,
    bool IsCompleted,
    TodoPriority Priority,
    string? Description,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc);

// Make the implicit Program class accessible for integration tests
public partial class Program { }
