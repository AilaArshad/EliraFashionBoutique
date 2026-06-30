using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Order_Item")]
public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int VariantId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal DiscountedAmount { get; set; } = 0.00m;

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey("OrderId")]
    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("VariantId")]
    public virtual ProductVariant? Variant { get; set; }
}
