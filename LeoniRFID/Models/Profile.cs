using Newtonsoft.Json;
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
    // [JsonIgnore] empêche Supabase (Newtonsoft.Json) d'essayer de les mapper
    // comme des colonnes de la base de données lors des INSERT/UPDATE.
    [JsonIgnore]
    public bool IsAdmin => Role == "Admin";

    [JsonIgnore]
    public bool IsMaintenance => Role == "Maintenance";

    [JsonIgnore]
    public string RoleDisplay => Role switch
    {
        "Admin"       => "👑 Administrator",
        "Maintenance" => "🔧 Maintenance Agent",
        _             => "👷 Technician"
    };

    [JsonIgnore]
    public string StatusDisplay => IsActive ? "✅ Active" : "❌ Disabled";
    
    // Génère les 2 initiales (ex: "Ahmed Ali" -> "AA")
    [JsonIgnore]
    public string Initials =>
        string.Join("", FullName.Split(' ')
            .Where(w => w.Length > 0)
            .Take(2)
            .Select(w => char.ToUpper(w[0]))).PadRight(2);
}
