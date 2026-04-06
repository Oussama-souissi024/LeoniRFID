using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

// 🎓 Pédagogie PFE : Modèle "Department" (Département d'usine)
// Cette classe représente un département physique dans l'usine LEONI (ex: LTN1, LTN2, LTN3).
// Chaque machine est rattachée à un département pour organiser le suivi RFID par zone.
[Table("departments")]
public class Department : BaseModel
{

    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }
}
