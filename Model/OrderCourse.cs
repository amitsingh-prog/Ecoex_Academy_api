using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class OrderCourse
    {
        [Key]
        public int OrderId { get; set; }
        public int CourseID { get; set; }

        public Order Order { get; set; } = null!;
        public Course Course { get; set; } = null!;

    }
}
