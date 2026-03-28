using Postgrest.Attributes;
using Postgrest.Models;

namespace LeoniRFID.Models;

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
