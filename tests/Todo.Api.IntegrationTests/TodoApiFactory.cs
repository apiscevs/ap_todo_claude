using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace Todo.Api.IntegrationTests;

public class TodoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestPassword = "P@ssw0rd123!";

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("tododb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string with test container
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:tododb"] = _dbContainer.GetConnectionString()
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext pool and factory
            services.RemoveAll(typeof(DbContextOptions<TodoDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<TodoDbContext>));

            // Remove Redis output caching and replace with in-memory caching for tests
            services.RemoveAll(typeof(Microsoft.AspNetCore.OutputCaching.IOutputCacheStore));
            services.AddOutputCache();

            // Add DbContext pool with test connection string
            services.AddDbContextPool<TodoDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // Add DbContext factory for GraphQL
            services.AddPooledDbContextFactory<TodoDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });

        builder.ConfigureServices(services =>
        {
            // Ensure database is migrated after all services are configured
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            db.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public async Task<TestAuthSession> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // First token is for anonymous principal so we can call /auth/register.
        await RefreshAntiforgeryHeaderAsync(client);

        var email = $"it-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = TestPassword,
            FirstName = "Integration",
            LastName = "Test"
        });
        registerResponse.EnsureSuccessStatusCode();

        var user = await registerResponse.Content.ReadFromJsonAsync<UserResponse>();
        if (user is null)
            throw new InvalidOperationException("Registration did not return a user payload.");

        // Refresh token after sign-in; ASP.NET antiforgery binds token to current principal.
        await RefreshAntiforgeryHeaderAsync(client);

        return new TestAuthSession(client, user.Id, user.Email);
    }

    public async Task<ApplicationUser> CreateUserAsync(string? email = null)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var resolvedEmail = email ?? $"db-{Guid.NewGuid():N}@example.com";

        var user = new ApplicationUser
        {
            UserName = resolvedEmail,
            Email = resolvedEmail
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create test user: {errors}");
        }

        return user;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
            throw new InvalidOperationException($"Missing Set-Cookie header for '{cookieName}'.");

        foreach (var header in cookieHeaders)
        {
            if (!header.StartsWith($"{cookieName}=", StringComparison.OrdinalIgnoreCase))
                continue;

            var rawValue = header[(cookieName.Length + 1)..];
            var separatorIndex = rawValue.IndexOf(';');
            var encodedValue = separatorIndex >= 0 ? rawValue[..separatorIndex] : rawValue;
            return Uri.UnescapeDataString(encodedValue);
        }

        throw new InvalidOperationException($"Cookie '{cookieName}' was not set.");
    }

    private static async Task RefreshAntiforgeryHeaderAsync(HttpClient client)
    {
        var csrfResponse = await client.GetAsync("/auth/csrf");
        csrfResponse.EnsureSuccessStatusCode();

        var csrfToken = ExtractCookieValue(csrfResponse, "XSRF-TOKEN");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-XSRF-TOKEN", csrfToken);
    }
}

public sealed record TestAuthSession(HttpClient Client, string UserId, string Email);
