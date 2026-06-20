using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EliraFashionBoutique.Models;

[Table("Customer")]
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNo { get; set; }

    [Required]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Gender { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [ForeignKey("UserId")]
    [JsonIgnore]
    public virtual User? User { get; set; }
}
