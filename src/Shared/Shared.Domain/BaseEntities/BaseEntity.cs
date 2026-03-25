namespace Shared.Domain.BaseEntities;

/// <summary>
/// Tüm domain entity'lerinin türediği temel sınıf.
/// Her entity benzersiz bir Guid Id'ye ve zaman damgalarına sahiptir.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    public void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
