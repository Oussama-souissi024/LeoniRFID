using SQLite;

namespace LeoniRFID.Models;

[Table("ScanEvents")]
public class ScanEvent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100), NotNull]
    public string TagId { get; set; } = string.Empty;

    public int MachineId { get; set; }
    public int UserId { get; set; }

    [MaxLength(30)]
    public string EventType { get; set; } = "Scan";         // Scan|Install|Remove|Maintenance

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsSynced { get; set; } = false;

    // Navigation (ignored by SQLite)
    [Ignore] public string? MachineName { get; set; }
    [Ignore] public string? UserFullName { get; set; }

    [Ignore]
    public string EventIcon => EventType switch
    {
        "Install"     => "📥",
        "Remove"      => "📤",
        "Maintenance" => "🔧",
        _             => "📡",
    };

    [Ignore]
    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
