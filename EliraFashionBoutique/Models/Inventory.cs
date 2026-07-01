using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Inventory")]
public class Inventory
{
    [Key]
    public int InventoryId { get; set; }

    public int VariantId { get; set; }

    public int QuantityAvailable { get; set; }

    public int ReorderLevel { get; set; } = 10;

    [ForeignKey("VariantId")]
    public virtual ProductVariant? Variant { get; set; }
}
