using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binah.Auth.Models;

/// <summary>
/// Client/contact record for CRM functionality
/// </summary>
[Table("clients")]
public class Client
{
    [Key]
    [Column("Id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("LastName")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("Phone")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [Column("Company")]
    public string? Company { get; set; }

    [Column("Address")]
    public string? Address { get; set; }

    [Column("Notes")]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Status")]
    public string Status { get; set; } = "active";

    [Column("Tags", TypeName = "jsonb")]
    public string Tags { get; set; } = "[]";

    [Column("CustomFields", TypeName = "jsonb")]
    public string CustomFields { get; set; } = "{}";

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    [Column("CreatedBy")]
    public string? CreatedBy { get; set; }

    [Column("DeletedAt")]
    public DateTime? DeletedAt { get; set; }
}
