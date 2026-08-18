using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; }
        public int EnrollmentId { get; set; }
        public int UserId { get; set; }
        public int CourseID { get; set; }
        public DateTime? IssuedAt { get; set; }
        public string? CertificateUrl { get; set; }

        public Enrollment Enrollment { get; set; } = null!;
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;

    }
}
