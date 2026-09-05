using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class User
    {

        [Key]
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Mobile { get; set; } = null!;
        public string? PasswordHash { get; set; }
        public string AuthProvider { get; set; } = null!;
        public string RegistrationType { get; set; } = null!;
        public string UserType { get; set; } = null!;
        public bool EmailVerified { get; set; }
        public bool MobileVerified { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<ZoomAccess> ZoomAccesses { get; set; } = new List<ZoomAccess>();
        //public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<RecordingAccess> RecordingAccesses { get; set; } = new List<RecordingAccess>();


    }
}
