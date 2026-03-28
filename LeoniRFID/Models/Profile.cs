using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "Technician";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("must_change_password")]
    public bool MustChangePassword { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsAdmin => Role == "Admin";
    public string RoleDisplay => Role == "Admin" ? "👑 Administrateur" : "🔧 Technicien";
    public string StatusDisplay => IsActive ? "✅ Actif" : "❌ Désactivé";
    public string Initials =>
        string.Join("", FullName.Split(' ')
            .Where(w => w.Length > 0)
            .Take(2)
            .Select(w => char.ToUpper(w[0]))).PadRight(2);
}
