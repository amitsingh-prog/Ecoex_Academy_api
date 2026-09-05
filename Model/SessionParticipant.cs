
using Ecoeex_Academy_Api.Model;
using Ecoex_Academy_Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ecoex_Academy_Api.Models
{
    public class SessionParticipant
    {
        [Key]
        public int Id { get; set; }

        public int UserID { get; set; }

        public int CourseID { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public string? ZoomLink { get; set; }

        public ZoomEmailStatus ZoomEmailStatus { get; set; } = ZoomEmailStatus.Pending;

        public DateTime? ZoomEmailSentAt { get; set; }

        public string? ZoomEmailResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;

        public virtual Course Course { get; set; } = null!;


    }
}