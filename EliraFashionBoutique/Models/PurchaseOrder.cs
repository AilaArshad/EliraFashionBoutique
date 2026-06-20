using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Purchase_Orders")]
public class PurchaseOrder
{
    [Key]
    public int PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }

    [Required]
    public DateTime ExpectedDeliveryDate { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending Audit";

    [Column(TypeName = "decimal(12, 2)")]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
