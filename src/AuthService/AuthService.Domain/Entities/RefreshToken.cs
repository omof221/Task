namespace AuthService.Domain.Entities;


public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual AppUser? User { get; set; }
    public bool IsActive => !IsRevoked && ExpiryDate > DateTime.UtcNow;
}
