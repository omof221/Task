namespace Shared.Domain.BaseEntities;


/// Tüm domain entity'lerinin türediği temel sınıf.
/// Domain invariantları factory/update metotlarıyla korunur;
/// EF Core'un property materialization'ı için setterlar public.

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
