using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Color")]
public class Color
{
    [Key]
    public int ColorId { get; set; }

    [Required]
    [StringLength(50)]
    public string ColorName { get; set; } = string.Empty;

    [StringLength(7)]
    public string? HexCode { get; set; }
}
