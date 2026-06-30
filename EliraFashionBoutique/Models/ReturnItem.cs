using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Return_Item")]
public class ReturnItem
{
    [Key]
    public int ReturnItemId { get; set; }

    [Required]
    public int ReturnId { get; set; }

    [Required]
    public int OrderItemId { get; set; }

    [Required]
    public int QuantityReturned { get; set; }

    [StringLength(100)]
    public string? ReturnedCondition { get; set; }

    [Required]
    [StringLength(100)]
    public string ResolutionType { get; set; } = "Full Refund to Bank";

    [ForeignKey("ReturnId")]
    [JsonIgnore]
    public virtual ReturnOrder ReturnOrder { get; set; } = null!;

    [ForeignKey("OrderItemId")]
    [JsonIgnore]
    public virtual OrderItem OrderItem { get; set; } = null!;
}
