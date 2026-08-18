using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecoeex_Academy_Api.Model
{

    [Table("tb_userdetail")]
    public class tb_userdetail
    {
        [Key]
        public int ID { get; set; }
        public string? UserName { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Status { get; set; }

        public DateTime? Createddate { get; set; }

    }
}


