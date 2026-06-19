using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Product")]
public class Product
{
    [Key]
    public int ProductId { get; set; }

    public int? SubCategoryId { get; set; }

    [Required]
    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal BasePrice { get; set; }

    [StringLength(100)]
    public string? SKU { get; set; }

    public bool? IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("SubCategoryId")]
    public virtual SubCategory? SubCategory { get; set; }

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
