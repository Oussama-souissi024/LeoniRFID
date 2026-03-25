using SQLite;

namespace LeoniRFID.Models;

[Table("Users")]
public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100), NotNull]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(150), Unique, NotNull]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20), NotNull]
    public string Role { get; set; } = "Technician";        // Admin | Technician

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    [Ignore]
    public bool IsAdmin => Role == "Admin";

    [Ignore]
    public string RoleDisplay => Role == "Admin" ? "👑 Administrateur" : "🔧 Technicien";

    [Ignore]
    public string Initials =>
        string.Join("", FullName.Split(' ')
            .Take(2)
            .Select(w => char.ToUpper(w[0]))).PadRight(2);
}
