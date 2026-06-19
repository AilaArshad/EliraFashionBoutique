using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Product_Variants")]
public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    public int? ProductId { get; set; }

    public int? SizeId { get; set; }

    public int? ColorId { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? VariantPrice { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? Weight { get; set; }

    [StringLength(100)]
    public string? VariantSKU { get; set; }

    public bool? IsActive { get; set; } = true;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("SizeId")]
    public virtual Size? Size { get; set; }

    [ForeignKey("ColorId")]
    public virtual Color? Color { get; set; }
}
