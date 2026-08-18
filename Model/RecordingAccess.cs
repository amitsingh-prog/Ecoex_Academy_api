using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class RecordingAccess
    {
        [Key]
        public int RecordingAccessId { get; set; }
        public int EnrollmentId { get; set; }
        public int UserId { get; set; }
        public int CourseID { get; set; }
        public string? RecordingUrl { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public Enrollment Enrollment { get; set; } = null!;
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;


    }
}
