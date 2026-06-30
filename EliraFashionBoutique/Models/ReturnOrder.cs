using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Return_Order")]
public class ReturnOrder
{
    [Key]
    public int ReturnId { get; set; }

    [Required]
    public int OrderId { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    public string ReturnStatus { get; set; } = "Pending";

    public string? CustomReasonText { get; set; }

    public int? ProcessedBy { get; set; }

    [ForeignKey("OrderId")]
    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
}
