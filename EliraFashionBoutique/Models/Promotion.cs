using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Promotion")]
public class Promotion
{
    [Key]
    public int PromotionId { get; set; }

    public int? SubCategoryId { get; set; }

    [StringLength(50)]
    public string? PromotionDiscount { get; set; }

    [StringLength(100)]
    public string? DiscountName { get; set; }

    [StringLength(50)]
    public string? DiscountType { get; set; }

    [Column(TypeName = "DATE")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "DATE")]
    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; } = true;

    [ForeignKey("SubCategoryId")]
    public virtual SubCategory? SubCategory { get; set; }
}
