using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Orders")]
public class Order
{
    [Key]
    public int OrderId { get; set; }

    public int? CustomerId { get; set; }

    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(150)]
    public string GuestEmail { get; set; } = string.Empty;

    [StringLength(20)]
    public string GuestPhoneNo { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime? DeliveryDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal DiscountedAmount { get; set; } = 0.00m;

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal FinalAmount { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string ShippingAddress { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual Payment? Payment { get; set; }

    [ForeignKey("CustomerId")]
    [JsonIgnore]
    public virtual Customer? Customer { get; set; }
}
