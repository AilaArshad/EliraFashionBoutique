using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Supplier")]
public class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [StringLength(150)]
    public string SupplierName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    [JsonIgnore]
    public virtual User? User { get; set; }

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
