using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Sub_Categories")]
public class SubCategory
{
    [Key]
    public int SubCategoryId { get; set; }

    public int? CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string SubcategoryName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SeasonType { get; set; } = string.Empty;

    [Column(TypeName = "DATE")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "DATE")]
    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; } = true;

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
