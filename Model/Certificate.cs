using System.ComponentModel.DataAnnotations;
using Ecoex_Academy_Api.Enums;
namespace Ecoex_Academy_Api.Model
{
    public class Certificate
    {
        [Key]
        public int Id { get; set; }

        public int ParticipantId { get; set; }

        public string CertificateId { get; set; } = null!;

        public int CourseID { get; set; }

        public string? CertificateFilePath { get; set; }

        public DateTime? IssuedAt { get; set; }

        public CertificateEmailStatus CertificateEmailStatus { get; set; } = CertificateEmailStatus.Pending;

        public DateTime? CertificateEmailSentAt { get; set; }

        public string? CertificateEmailResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }


    }
}
