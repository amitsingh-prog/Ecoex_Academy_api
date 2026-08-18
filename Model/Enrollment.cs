using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int CourseID { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Order Order { get; set; } = null!;
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;

        public ICollection<ZoomAccess> ZoomAccesses { get; set; } = new List<ZoomAccess>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<RecordingAccess> RecordingAccesses { get; set; } = new List<RecordingAccess>();


    }
}
