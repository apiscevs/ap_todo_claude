using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<TodoDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "todo_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "todo_antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

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
              .AllowAnyMethod()
              .AllowCredentials());
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
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method))
    {
        if (!context.Request.Path.StartsWithSegments("/auth/csrf") &&
            !context.Request.Path.StartsWithSegments("/graphql"))
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(context);
        }
    }

    await next();
});

app.MapGet("/auth/csrf", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Lax,
        Secure = !builder.Environment.IsDevelopment(),
        Path = "/"
    });
    return Results.NoContent();
});

app.MapPost("/auth/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName
    };

    var result = await userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
        return Results.BadRequest(result.Errors.Select(e => e.Description));

    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.Ok(new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName));
});

app.MapPost("/auth/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
        return Results.BadRequest(new { error = "Invalid credentials" });

    var result = await signInManager.PasswordSignInAsync(user, request.Password, true, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Results.BadRequest(new { error = "Invalid credentials" });

    return Results.Ok(new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName));
});

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/auth/me", async (ClaimsPrincipal user, UserManager<ApplicationUser> userManager) =>
{
    var userId = userManager.GetUserId(user);
    if (userId is null)
        return Results.Unauthorized();

    var currentUser = await userManager.FindByIdAsync(userId);
    if (currentUser is null)
        return Results.Unauthorized();

    return Results.Ok(new UserResponse(currentUser.Id, currentUser.Email!, currentUser.FirstName, currentUser.LastName));
}).RequireAuthorization();

app.MapGet("/api/todos", async (
    TodoDbContext db,
    UserManager<ApplicationUser> userManager,
    ClaimsPrincipal user) =>
{
    var userId = userManager.GetUserId(user);
    if (userId is null)
        return Results.Unauthorized();

    var todos = await db.Todos
        .Where(t => t.UserId == userId)
        .OrderBy(t => t.Id)
        .ToListAsync();
    return Results.Ok(todos);
}).RequireAuthorization();

app.MapPost("/api/todos", async (
    CreateTodoRequest request,
    TodoDbContext db,
    UserManager<ApplicationUser> userManager,
    ClaimsPrincipal user,
    IOutputCacheStore cache,
    CancellationToken ct) =>
{
    var userId = userManager.GetUserId(user);
    if (userId is null)
        return Results.Unauthorized();

    var (startAtUtc, endAtUtc) = TodoSchedule.Normalize(request.StartAtUtc, request.EndAtUtc);
    if (!TodoSchedule.TryValidate(startAtUtc, endAtUtc, out var error))
        return Results.BadRequest(new { error });

    var todo = new TodoItem
    {
        Title = request.Title,
        Description = request.Description ?? string.Empty,
        Priority = request.Priority,
        StartAtUtc = startAtUtc,
        EndAtUtc = endAtUtc,
        UserId = userId
    };
    db.Todos.Add(todo);
    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.Created($"/api/todos/{todo.Id}", todo);
}).RequireAuthorization();

app.MapPut("/api/todos/{id}/toggle", async (
    int id,
    TodoDbContext db,
    UserManager<ApplicationUser> userManager,
    ClaimsPrincipal user,
    IOutputCacheStore cache,
    CancellationToken ct) =>
{
    var userId = userManager.GetUserId(user);
    if (userId is null)
        return Results.Unauthorized();

    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
    if (todo is null)
        return Results.NotFound();

    todo.IsCompleted = !todo.IsCompleted;
    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.Ok(todo);
}).RequireAuthorization();

app.MapDelete("/api/todos/{id}", async (
    int id,
    TodoDbContext db,
    UserManager<ApplicationUser> userManager,
    ClaimsPrincipal user,
    IOutputCacheStore cache,
    CancellationToken ct) =>
{
    var userId = userManager.GetUserId(user);
    if (userId is null)
        return Results.Unauthorized();

    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
    if (todo is null)
        return Results.NotFound();

    db.Todos.Remove(todo);
    await db.SaveChangesAsync(ct);
    await cache.EvictByTagAsync("todos", ct);
    return Results.NoContent();
}).RequireAuthorization();

app.MapPut("/api/todos/{id}", async (
    int id,
    UpdateTodoRequest request,
    TodoDbContext db,
    UserManager<ApplicationUser> userManager,
    ClaimsPrincipal user,
    IOutputCacheStore cache,
    CancellationToken ct) =>
{
    var userId = userManager.GetUserId(user);
    if (userId is null)
        return Results.Unauthorized();

    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
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
}).RequireAuthorization();

// Keep resolver-level auth checks while allowing unauthenticated schema introspection for tooling (codegen).
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

public record RegisterRequest(string Email, string Password, string? FirstName, string? LastName);
public record LoginRequest(string Email, string Password);
public record UserResponse(string Id, string Email, string? FirstName, string? LastName);

// Make the implicit Program class accessible for integration tests
public partial class Program { }
