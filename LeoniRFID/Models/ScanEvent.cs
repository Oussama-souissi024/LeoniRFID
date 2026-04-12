using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

// 🎓 Pédagogie PFE : Modèle "ScanEvent" (Événement de Scan RFID)
// Chaque fois qu'un technicien passe un lecteur RFID devant une machine,
// un "ScanEvent" est créé et enregistré dans Supabase. C'est le journal
// de traçabilité complet qui permet de savoir QUI a scanné QUOI et QUAND.
[Table("scan_events")]
public class ScanEvent : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("tag_id")]
    public string TagId { get; set; } = string.Empty;

    [Column("machine_id")]
    public int MachineId { get; set; }

    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = "Scan";

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("notes")]
    public string? Notes { get; set; }

    // 🎓 Propriétés calculées pour l'affichage XAML
    // [JsonIgnore] empêche Supabase de les envoyer à la DB
    [JsonIgnore]
    public string EventIcon => EventType switch
    {
        "Install"     => "📥",
        "Remove"      => "📤",
        "Maintenance" => "🔧",
        "Registered"  => "🆕",
        _             => "📡",
    };

    [JsonIgnore]
    public string TimestampDisplay =>
        Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
