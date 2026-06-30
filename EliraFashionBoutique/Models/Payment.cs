using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Payment")]
public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [StringLength(50)]
    public string PaymentStatus { get; set; } = "Pending";

    public DateTime PaidAt { get; set; } = DateTime.Now;

    [ForeignKey("OrderId")]
    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
}
