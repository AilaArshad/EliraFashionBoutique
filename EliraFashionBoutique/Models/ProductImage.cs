using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Product_Images")]
public class ProductImage
{
    [Key]
    public int ImageId { get; set; }

    public int? ProductId { get; set; }

    public int? ColorId { get; set; }

    [Required]
    [StringLength(255)]
    public string ImageURL { get; set; } = string.Empty;

    public bool? IsPrimary { get; set; } = true;

    public int? DisplayOrder { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ColorId")]
    public virtual Color? Color { get; set; }
}
