using Microsoft.EntityFrameworkCore;

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

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Medium is the default priority to balance between urgent and low-priority tasks
        modelBuilder.Entity<TodoItem>()
            .Property(t => t.Priority)
            .HasConversion<int>()  // Store as integer for correct ordering
            .HasDefaultValue(TodoPriority.Medium);

        modelBuilder.Entity<TodoItem>()
            .Property(t => t.Description)
            .HasMaxLength(1000)
            .HasDefaultValue("");
    }
}

public class TodoItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    public string? Description { get; set; }
}
