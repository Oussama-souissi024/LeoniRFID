using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

[Table("machines")]
public class Machine : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("tag_reference")]
    public string TagReference { get; set; } = string.Empty;

    [Column("standard_equipment_name")]
    public string StandardEquipmentName { get; set; } = string.Empty;

    [Column("plant")]
    public string Plant { get; set; } = string.Empty;

    [Column("area")]
    public string Area { get; set; } = string.Empty;

    [Column("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [Column("immobilisation_number")]
    public string ImmobilisationNumber { get; set; } = string.Empty;

    [Column("cao_number")]
    public string? CaoNumber { get; set; }

    [Column("year_of_construction")]
    public int YearOfConstruction { get; set; }

    [Column("equipment_status")]
    public string EquipmentStatus { get; set; } = "Active";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // 🎓 Pédagogie PFE : Propriété calculée — Âge dynamique
    [JsonIgnore]
    public int Age => DateTime.Now.Year - YearOfConstruction;

    // 🎓 Pédagogie PFE : Propriétés calculées pour l'affichage XAML
    // [JsonIgnore] de Newtonsoft.Json empêche Supabase (qui utilise Newtonsoft)
    // d'essayer d'insérer ces propriétés comme des colonnes DB.
    [JsonIgnore]
    public string StatusDisplay => EquipmentStatus switch
    {
        "Active"            => "✅ Active",
        "Passive"           => "⏸️ Passive",
        "Defect"            => "🔴 Defect",
        "Scrapped"          => "❌ Scrapped",
        "TransferDone"      => "🔄 Transfer Done",
        "TransferOngoing"   => "🔃 Transfer Ongoing",
        "TransferAvailable" => "📦 Transfer Available",
        _                   => EquipmentStatus
    };

    [JsonIgnore]
    public string YearDisplay =>
        YearOfConstruction > 0
            ? $"{YearOfConstruction} ({Age} ans)"
            : "—";

    // Aliases de compatibilité pour les ViewModels existants (ScanEvent utilise TagId)
    [JsonIgnore]
    public string Name => StandardEquipmentName;

    [JsonIgnore]
    public string Status
    {
        get => EquipmentStatus;
        set => EquipmentStatus = value;
    }

    [JsonIgnore]
    public string Department => Plant;

    [JsonIgnore]
    public string TagId => TagReference;

    [JsonIgnore]
    public string LastUpdatedDisplay =>
        LastUpdated != default
            ? LastUpdated.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : "—";
}
