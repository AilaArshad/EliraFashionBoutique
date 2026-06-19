using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Sizes")]
public class Size
{
    [Key]
    public int SizeId { get; set; }

    [Required]
    [StringLength(20)]
    public string SizeName { get; set; } = string.Empty;
}
