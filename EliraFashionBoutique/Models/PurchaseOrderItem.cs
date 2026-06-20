using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Purchase_Order_Item")]
public class PurchaseOrderItem
{
    [Key]
    public int PurchaseOrderItemId { get; set; }

    public int PurchaseOrderId { get; set; }

    public int VariantId { get; set; }

    public int QuantityOrdered { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey("PurchaseOrderId")]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("VariantId")]
    public virtual ProductVariant? Variant { get; set; }
}
