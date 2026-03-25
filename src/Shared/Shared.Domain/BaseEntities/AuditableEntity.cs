namespace Shared.Domain.BaseEntities;

/// <summary>
/// Kimin oluşturduğunu / güncellediğini takip eden audit bilgisi taşıyan entity.
/// SRP: Audit sorumluluğunu BaseEntity'den ayırarak bu sınıfa taşıdık.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    public void SetCreatedBy(string userId) => CreatedBy = userId;
    public void SetUpdatedBy(string userId)
    {
        UpdatedBy = userId;
        SetUpdatedAt();
    }
}
