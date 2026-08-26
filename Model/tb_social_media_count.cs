using System;
using System.ComponentModel.DataAnnotations;

namespace Ecoex_Academy_Api.Model
{
    public class tb_social_media_count
    {
        [Key]
        public int Id { get; set; }
        public String SocialMedia { get; set; }
        public int VisitingCount { get; set; }
        public DateTime? lastUpdate { get; set; }
        public String Status { get; set; }



    }
}
