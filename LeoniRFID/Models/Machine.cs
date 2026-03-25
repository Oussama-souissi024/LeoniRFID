using SQLite;

namespace LeoniRFID.Models;

[Table("Machines")]
public class Machine
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100), NotNull]
    public string TagId { get; set; } = string.Empty;        // EPC RFID

    [MaxLength(150), NotNull]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Department { get; set; } = string.Empty;   // LTN1/LTN2/LTN3

    [MaxLength(30)]
    public string Status { get; set; } = "Installed";        // Installed|Removed|Maintenance

    public DateTime InstallationDate { get; set; } = DateTime.Now;
    public DateTime? ExitDate { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public bool IsSynced { get; set; } = false;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [Ignore]
    public string StatusDisplay => Status switch
    {
        "Installed"   => "✅ Installé",
        "Removed"     => "❌ Retiré",
        "Maintenance" => "🔧 Maintenance",
        _             => Status
    };

    [Ignore]
    public string InstallationDateDisplay =>
        InstallationDate != default ? InstallationDate.ToString("dd/MM/yyyy") : "—";
}
