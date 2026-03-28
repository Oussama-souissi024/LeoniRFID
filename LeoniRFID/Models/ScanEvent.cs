using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

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
}
