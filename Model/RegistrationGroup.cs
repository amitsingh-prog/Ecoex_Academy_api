using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecoeex_Academy_Api.Model
{
    [Table("tb_RegistrationGroups")]
    public class RegistrationGroup
    {
        [Key]
        public int GroupId { get; set; }

        [Required]
        [StringLength(50)]
        public string GroupCode { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation Property
        public ICollection<RegistrationGroupMember> Members { get; set; }
            = new List<RegistrationGroupMember>();
    }
}
