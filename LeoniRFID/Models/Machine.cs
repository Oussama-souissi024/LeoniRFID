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
    public string Status { get; set; } = "Installed";

    [Column("installation_date")]
    public DateTime InstallationDate { get; set; } = DateTime.Now;

    [Column("exit_date")]
    public DateTime? ExitDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string StatusDisplay => Status switch
    {
        "Installed"   => "✅ Installé",
        "Removed"     => "❌ Retiré",
        "Maintenance" => "🔧 Maintenance",
        _             => Status
    };

    public string InstallationDateDisplay =>
        InstallationDate != default
            ? InstallationDate.ToString("dd/MM/yyyy")
            : "—";
}
