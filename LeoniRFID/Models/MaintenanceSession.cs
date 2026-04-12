using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

// 🎓 Pédagogie PFE : Modèle "MaintenanceSession" (Session de Maintenance)
// Chaque fois qu'un agent de maintenance commence une intervention sur une machine,
// une "MaintenanceSession" est créée. Elle enregistre :
// - L'heure de début (started_at)
// - L'heure de fin (ended_at) — NULL tant que la maintenance est en cours
// - La durée calculée en minutes (duration_minutes)
// Le timer affiché sur l'écran du Zebra est calculé en temps réel depuis started_at.
[Table("maintenance_sessions")]
public class MaintenanceSession : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("machine_id")]
    public int MachineId { get; set; }

    // 🎓 L'ID de l'agent de maintenance qui effectue l'intervention
    [Column("technician_id")]
    public string? TechnicianId { get; set; }

    // 🎓 Timestamp UTC du début de la maintenance
    // C'est cette valeur qui est utilisée pour calculer le timer en temps réel
    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    // 🎓 NULL = maintenance en cours. Rempli quand l'agent clique "Maintenance Terminée"
    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    // 🎓 Calculé automatiquement à la fin : (ended_at - started_at).TotalMinutes
    [Column("duration_minutes")]
    public double? DurationMinutes { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ── Propriétés calculées pour l'affichage XAML ──────────────────────
    // [JsonIgnore] empêche Supabase (Newtonsoft.Json) de les envoyer à la DB

    [JsonIgnore]
    public string DurationDisplay => DurationMinutes.HasValue
        ? TimeSpan.FromMinutes(DurationMinutes.Value).ToString(@"hh\hmm\m\i\n")
        : "⏱️ En cours…";

    [JsonIgnore]
    public bool IsActive => EndedAt is null;

    [JsonIgnore]
    public string StartedAtDisplay =>
        StartedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");

    [JsonIgnore]
    public string EndedAtDisplay =>
        EndedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "—";
}
