using System.ComponentModel.DataAnnotations;

namespace Ecoeex_Academy_Api.Model
{
    public class PricingSetting
    {
        [Key]
        public int SettingId { get; set; }
        public decimal BundleAllCoursesPrice { get; set; }
        public decimal GroupDiscountPercent { get; set; }
        public int GroupMinSizeForDiscount { get; set; }

    }
}
