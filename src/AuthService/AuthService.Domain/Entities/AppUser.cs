using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
