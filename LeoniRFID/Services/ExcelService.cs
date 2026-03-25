using ClosedXML.Excel;
using LeoniRFID.Models;

namespace LeoniRFID.Services;

public class ExcelService
{
    // ── Import ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Parse a LEONI machine Excel file.
    /// Expected columns: TagID | MachineName | Department | Status | InstallationDate
    /// </summary>
    public List<Machine> ImportMachines(Stream excelStream)
    {
        var machines = new List<Machine>();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1); // skip header

        if (rows is null) return machines;

        foreach (var row in rows)
        {
            try
            {
                var tagId  = row.Cell(1).GetString().Trim();
                var name   = row.Cell(2).GetString().Trim();
                var dept   = row.Cell(3).GetString().Trim().ToUpper();
                var status = row.Cell(4).GetString().Trim();
                var dateStr = row.Cell(5).GetString().Trim();

                if (string.IsNullOrWhiteSpace(tagId) || string.IsNullOrWhiteSpace(name))
                    continue;

                DateTime.TryParse(dateStr, out var installDate);

                // Normalise status
                status = status.ToLower() switch
                {
                    "installed"   or "installé"   or "installe"   => "Installed",
                    "removed"     or "retiré"     or "retire"     => "Removed",
                    "maintenance"                                  => "Maintenance",
                    _                                              => "Installed"
                };

                // Normalise dept
                dept = dept switch
                {
                    "LTN1" or "LTN2" or "LTN3" => dept,
                    _ => "LTN1"
                };

                machines.Add(new Machine
                {
                    TagId            = tagId,
                    Name             = name,
                    Department       = dept,
                    Status           = status,
                    InstallationDate = installDate != default ? installDate : DateTime.Now,
                    IsSynced         = false
                });
            }
            catch { /* Skip malformed rows */ }
        }

        return machines;
    }

    // ── Export ────────────────────────────────────────────────────────────────
    public Stream ExportReport(List<Machine> machines, List<ScanEvent> events)
    {
        using var workbook = new XLWorkbook();

        // ── Sheet 1 : Machines ──────────────────────────────────────────────
        var ws1 = workbook.Worksheets.Add("Machines");
        ApplyHeaderStyle(ws1, new[]
        {
            "Tag ID (EPC)", "Nom Machine", "Département",
            "Statut", "Date Installation", "Date Retrait", "Notes"
        });

        int row = 2;
        foreach (var m in machines)
        {
            ws1.Cell(row, 1).Value = m.TagId;
            ws1.Cell(row, 2).Value = m.Name;
            ws1.Cell(row, 3).Value = m.Department;
            ws1.Cell(row, 4).Value = m.Status;
            ws1.Cell(row, 5).Value = m.InstallationDate.ToString("dd/MM/yyyy");
            ws1.Cell(row, 6).Value = m.ExitDate?.ToString("dd/MM/yyyy") ?? "";
            ws1.Cell(row, 7).Value = m.Notes ?? "";

            // Colour status cell
            var statusCell = ws1.Cell(row, 4);
            statusCell.Style.Fill.BackgroundColor = m.Status switch
            {
                "Installed"   => XLColor.FromHtml("#2ECC71"),
                "Removed"     => XLColor.FromHtml("#E74C3C"),
                "Maintenance" => XLColor.FromHtml("#F39C12"),
                _             => XLColor.White
            };
            statusCell.Style.Font.FontColor = XLColor.White;
            statusCell.Style.Font.Bold = true;
            row++;
        }
        ws1.Columns().AdjustToContents();

        // ── Sheet 2 : Scan Events ───────────────────────────────────────────
        var ws2 = workbook.Worksheets.Add("Événements RFID");
        ApplyHeaderStyle(ws2, new[]
        {
            "Horodatage", "Tag ID", "Machine", "Type Événement", "Opérateur", "Notes"
        });

        row = 2;
        foreach (var e in events)
        {
            ws2.Cell(row, 1).Value = e.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            ws2.Cell(row, 2).Value = e.TagId;
            ws2.Cell(row, 3).Value = e.MachineName ?? "—";
            ws2.Cell(row, 4).Value = e.EventType;
            ws2.Cell(row, 5).Value = e.UserFullName ?? "—";
            ws2.Cell(row, 6).Value = e.Notes ?? "";
            row++;
        }
        ws2.Columns().AdjustToContents();

        // ── Title Style ─────────────────────────────────────────────────────
        var titleStyle = workbook.Style;

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    // ── Generate Test Data Excel ──────────────────────────────────────────────
    public Stream GenerateTestExcel()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Machines");
        ApplyHeaderStyle(ws, new[]
        {
            "TagID", "MachineName", "Department", "Status", "InstallationDate"
        });

        var depts   = new[] { "LTN1", "LTN2", "LTN3" };
        var statuses = new[] { "Installed", "Installed", "Installed", "Removed", "Maintenance" };
        var rng     = new Random(42);

        for (int i = 0; i < 26; i++)
        {
            char letter = (char)('A' + i);
            var dept   = depts[i % 3];
            var status = statuses[rng.Next(statuses.Length)];
            var date   = DateTime.Now.AddDays(-rng.Next(30, 365));
            var epc    = $"E200001722110{(1000 + i):D4}{rng.Next(10000, 99999)}";

            ws.Cell(i + 2, 1).Value = epc;
            ws.Cell(i + 2, 2).Value = $"Equipement{letter}";
            ws.Cell(i + 2, 3).Value = dept;
            ws.Cell(i + 2, 4).Value = status;
            ws.Cell(i + 2, 5).Value = date.ToString("yyyy-MM-dd");
        }

        ws.Columns().AdjustToContents();
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void ApplyHeaderStyle(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#00205B");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }
}
