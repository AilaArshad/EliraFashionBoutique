using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Refund")]
public class Refund
{
    [Key]
    public int RefundId { get; set; }

    [Required]
    public int ReturnId { get; set; }

    public int? PaymentId { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal RefundAmount { get; set; }

    [Required]
    [StringLength(50)]
    public string RefundMethod { get; set; } = "Bank Transfer";

    [StringLength(50)]
    public string? RefundStatus { get; set; }

    [Column(TypeName = "date")]
    public DateTime? RefundDate { get; set; }

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [ForeignKey("ReturnId")]
    [JsonIgnore]
    public virtual ReturnOrder ReturnOrder { get; set; } = null!;

    [ForeignKey("PaymentId")]
    [JsonIgnore]
    public virtual Payment? Payment { get; set; }
}
