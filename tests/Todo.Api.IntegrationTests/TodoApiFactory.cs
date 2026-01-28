using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Todo.Api.IntegrationTests;

public class TodoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
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
}
