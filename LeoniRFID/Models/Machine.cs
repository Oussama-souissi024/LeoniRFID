using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

[Table("machines")]
public class Machine : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("tag_id")]
    public string TagId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("department")]
    public string Department { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "Running";

    [Column("installation_date")]
    public DateTime InstallationDate { get; set; } = DateTime.Now;

    // 🎓 Pédagogie PFE : Nullable Types
    [Column("exit_date")]
    public DateTime? ExitDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // 🎓 Pédagogie PFE : Propriétés calculées pour l'affichage XAML
    // [JsonIgnore] de Newtonsoft.Json empêche Supabase (qui utilise Newtonsoft)
    // d'essayer d'insérer ces propriétés comme des colonnes DB.
    [JsonIgnore]
    public string StatusDisplay => Status switch
    {
        "Running"       => "✅ En Marche",
        "Broken"        => "🔴 En Panne",
        "InMaintenance" => "🔧 Maintenance en cours",
        "Paused"        => "⏸️ En Pause",
        "Removed"       => "❌ Retiré",
        _               => Status
    };

    [JsonIgnore]
    public string InstallationDateDisplay =>
        InstallationDate != default
            ? InstallationDate.ToString("dd/MM/yyyy")
            : "—";
}
