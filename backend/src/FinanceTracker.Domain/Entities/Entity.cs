namespace FinanceTracker.Domain.Entities;

public abstract class Entity
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Deleted { get; set; } = false;
}
