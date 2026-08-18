using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class OtpRequest
    {
        [Key]
        public int OtpRequestId { get; set; }
        public string TargetType { get; set; } = null!;
        public string TargetValue { get; set; } = null!;
        public string OtpCodeHash { get; set; } = null!;
        public string Purpose { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
