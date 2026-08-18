using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class AdminUser
    {
        [Key]
        public int AdminUserId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiry { get; set; }

    }
}
