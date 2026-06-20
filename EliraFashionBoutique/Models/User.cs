using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliraFashionBoutique.Models;

[Table("Users")]
public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;

    public bool? IsEmailVerified { get; set; } = true;

    public DateTime? LastLogin { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Supplier? Supplier { get; set; }
}
