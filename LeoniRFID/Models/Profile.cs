using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

// 🎓 Pédagogie PFE : Mapping ORM Supabase
// L'attribut [Table("profiles")] indique à Supabase que cette classe C# 
// correspond exactement à la table "profiles" dans PostgreSQL.
[Table("profiles")]
public class Profile : BaseModel
{
    // 🎓 L'attribut PrimaryKey indique que "id" est la clé primaire.
    // Le `false` signifie que l'ID n'est pas auto-généré par la DB de façon incrémentale 
    // (on utilise ici un UUID généré par le système d'authentification).
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "Technician";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // 🎓 Ce booléen est le cœur de notre système de "Zero-Knowledge Password".
    // Il force l'utilisateur à créer son mot de passe lors de sa première connexion.
    [Column("must_change_password")]
    public bool MustChangePassword { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 🎓 Pédagogie PFE : Propriétés Calculées (Computed Properties)
    // Ces propriétés n'ont pas l'attribut [Column], elles NE SONT DONC PAS sauvegardées 
    // dans la base de données. Elles servent juste à formater l'affichage dans l'interface (XAML).
    public bool IsAdmin => Role == "Admin";
    public string RoleDisplay => Role == "Admin" ? "👑 Administrateur" : "🔧 Technicien";
    public string StatusDisplay => IsActive ? "✅ Actif" : "❌ Désactivé";
    
    // Génère les 2 initiales (ex: "Ahmed Ali" -> "AA")
    public string Initials =>
        string.Join("", FullName.Split(' ')
            .Where(w => w.Length > 0)
            .Take(2)
            .Select(w => char.ToUpper(w[0]))).PadRight(2);
}
