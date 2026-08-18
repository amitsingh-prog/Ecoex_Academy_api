using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class ZoomAccess
    {

        [Key]
        public int ZoomAccessId { get; set; }
        public int EnrollmentId { get; set; }
        public int UserId { get; set; }
        public string ZoomMeetingId { get; set; } = null!;
        public string ZoomRegistrantId { get; set; } = null!;
        public string JoinUrl { get; set; } = null!;
        public DateTime? EmailSentAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public Enrollment Enrollment { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
