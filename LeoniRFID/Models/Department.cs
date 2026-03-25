using SQLite;

namespace LeoniRFID.Models;

[Table("Departments")]
public class Department
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(10), Unique, NotNull]
    public string Code { get; set; } = string.Empty;        // LTN1 / LTN2 / LTN3

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    [Ignore]
    public static List<Department> DefaultDepartments =>
    [
        new() { Code = "LTN1", Name = "Atelier LTN1", Description = "Ligne de production 1" },
        new() { Code = "LTN2", Name = "Atelier LTN2", Description = "Ligne de production 2" },
        new() { Code = "LTN3", Name = "Atelier LTN3", Description = "Ligne de production 3" },
    ];
}
