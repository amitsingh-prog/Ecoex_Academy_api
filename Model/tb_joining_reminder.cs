using Ecoex_Academy_Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ecoex_Academy_Api.Model
{
    public class tb_joining_reminder
    {
        [Key]
        public int ID { get; set; }
        public int SessionParticipantID { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        public string? ReminderEmailResponse { get; set; }
        public ZoomEmailStatus? ReminderEmailStatus { get; set; }

    }
}
