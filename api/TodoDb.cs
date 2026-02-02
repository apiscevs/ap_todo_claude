using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

/// <summary>
/// Represents the priority level of a todo item.
/// Stored as integer in database for correct ordering.
/// </summary>
public enum TodoPriority
{
    /// <summary>Low priority (value: 1)</summary>
    Low = 1,
    /// <summary>Medium priority - default value (value: 2)</summary>
    Medium = 2,
    /// <summary>High priority (value: 3)</summary>
    High = 3
}

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var todo = modelBuilder.Entity<TodoItem>();

        // Medium is the default priority to balance between urgent and low-priority tasks
        todo.Property(t => t.Priority)
            .HasConversion<int>()  // Store as integer for correct ordering
            .HasDefaultValue(TodoPriority.Medium);

        todo.Property(t => t.Description)
            .HasMaxLength(1000)
            .HasDefaultValue("");

        todo.Property(t => t.StartAtUtc)
            .HasColumnType("timestamp with time zone");

        todo.Property(t => t.EndAtUtc)
            .HasColumnType("timestamp with time zone");

        todo.ToTable(t => t.HasCheckConstraint(
            "CK_Todos_Schedule",
            "(\"StartAtUtc\" IS NULL AND \"EndAtUtc\" IS NULL) OR " +
            "(\"StartAtUtc\" IS NOT NULL AND \"EndAtUtc\" IS NOT NULL AND \"EndAtUtc\" >= \"StartAtUtc\")"));
    }
}

public class TodoItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    public string? Description { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    [GraphQLIgnore]
    public string UserId { get; set; } = string.Empty;
    [GraphQLIgnore]
    public ApplicationUser? User { get; set; }
}
