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

    public string EventIcon => EventType switch
    {
        "Install"     => "📥",
        "Remove"      => "📤",
        "Maintenance" => "🔧",
        _             => "📡",
    };

    public string TimestampDisplay =>
        Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
    // Commentaire pédagogique :
    // - Les propriétés calculées comme `EventIcon` ou `TimestampDisplay` facilitent l'affichage dans les DataTemplates XAML.
    // - Keep UI-friendly formatting in ViewModels/Models to avoid dupliquer la logique dans le XAML.
}
