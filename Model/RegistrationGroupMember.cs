using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecoeex_Academy_Api.Model
{
    [Table("tb_RegistrationGroupMembers")]
    public class RegistrationGroupMember
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }

        public int PrimaryUserId { get; set; }

        public int MemberUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(GroupId))]
        public RegistrationGroup? Group { get; set; }

        [ForeignKey(nameof(PrimaryUserId))]
        public User? PrimaryUser { get; set; }

        [ForeignKey(nameof(MemberUserId))]
        public User? MemberUser { get; set; }
    }
}
