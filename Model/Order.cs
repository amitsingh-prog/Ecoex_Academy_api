using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int PayerUserId { get; set; }
        public int? GroupId { get; set; }
        public string OrderType { get; set; } = null!;
        public int GroupSizeAtPurchase { get; set; }
        public decimal PerPersonAmount { get; set; }
        public decimal DiscountPercentApplied { get; set; }
        public decimal CombinedOffer { get; set; }
        public decimal GstRate { get; set; }
        public decimal GstAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public User PayerUser { get; set; } = null!;
        public RegistrationGroup? Group { get; set; }

        public ICollection<OrderCourse> OrderCourses { get; set; } = new List<OrderCourse>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

        public Payment? Payment { get; set; }
    }
}
