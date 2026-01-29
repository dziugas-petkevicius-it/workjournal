using Microsoft.Data.Sqlite;
using WorkJournal.Entities;
using ClosedXML.Excel;

namespace WorkJournal.Services;

public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = GetDatabasePath();
        InitializeDatabase();
    }

    private string GetDatabasePath()
    {
#if ANDROID || IOS
        // Mobile platforms MUST use sandboxed storage
        return Path.Combine(FileSystem.AppDataDirectory, "workcalendar.db");
#else
    // Desktop platforms → next to executable
    var exePath = AppContext.BaseDirectory;
    return Path.Combine(exePath, "workcalendar.db");
#endif
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS WeekData (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            WeekStart TEXT NOT NULL UNIQUE,
            SaturdayHours INTEGER,
            SundayHours INTEGER,
            KilometersDriven INTEGER,
            DaysWorked INTEGER,
            HoursDriven INTEGER,
            OtherWork TEXT,
            TotalWorked TEXT,
            Paid TEXT,
            Comment TEXT,
            IsVacationWeek INTEGER NOT NULL
        )";
        command.ExecuteNonQuery();
    }

    public async Task SaveWeekDataAsync(WeekFormData form, DateTime weekStart)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        INSERT INTO WeekData
        (WeekStart, SaturdayHours, SundayHours, KilometersDriven, DaysWorked,
         HoursDriven, OtherWork, TotalWorked, Paid, Comment, IsVacationWeek)
        VALUES
        ($weekStart, $sat, $sun, $km, $days,
         $hours, $other, $total, $paid, $comment, $vacation)
        ON CONFLICT(WeekStart) DO UPDATE SET
            SaturdayHours = excluded.SaturdayHours,
            SundayHours = excluded.SundayHours,
            KilometersDriven = excluded.KilometersDriven,
            DaysWorked = excluded.DaysWorked,
            HoursDriven = excluded.HoursDriven,
            OtherWork = excluded.OtherWork,
            TotalWorked = excluded.TotalWorked,
            Paid = excluded.Paid,
            Comment = excluded.Comment,
            IsVacationWeek = excluded.IsVacationWeek;
    ";

        command.Parameters.AddWithValue("$weekStart", weekStart.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$sat", form.SaturdayHours);
        command.Parameters.AddWithValue("$sun", form.SundayHours);
        command.Parameters.AddWithValue("$km", form.KilometersDriven);
        command.Parameters.AddWithValue("$days", form.DaysWorked);
        command.Parameters.AddWithValue("$hours", form.HoursDriven);
        command.Parameters.AddWithValue("$other", form.OtherWork ?? "");
        command.Parameters.AddWithValue("$total", form.TotalWorked ?? "");
        command.Parameters.AddWithValue("$paid", form.Paid ?? "");
        command.Parameters.AddWithValue("$comment", form.Comment ?? "");
        command.Parameters.AddWithValue("$vacation", form.IsVacationWeek ? 1 : 0);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<WeekFormData?> LoadWeekDataAsync(DateTime weekStart)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT SaturdayHours, SundayHours, KilometersDriven, DaysWorked,
               HoursDriven, OtherWork, TotalWorked, Paid, Comment, IsVacationWeek
        FROM WeekData
        WHERE WeekStart = $weekStart
        LIMIT 1";
        command.Parameters.AddWithValue("$weekStart", weekStart.ToString("yyyy-MM-dd"));

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WeekFormData
            {
                SaturdayHours = reader.GetInt32(0),
                SundayHours = reader.GetInt32(1),
                KilometersDriven = reader.GetInt32(2),
                DaysWorked = reader.GetInt32(3),
                HoursDriven = reader.GetInt32(4),
                OtherWork = reader.IsDBNull(5) ? "" : reader.GetString(5),
                TotalWorked = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Paid = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Comment = reader.IsDBNull(8) ? "" : reader.GetString(8),
                IsVacationWeek = reader.GetInt32(9) == 1
            };
        }

        return null;
    }

    public async Task<string> ExportYearToExcelAsync(int year)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT WeekStart, SaturdayHours, SundayHours, KilometersDriven, DaysWorked,
               HoursDriven, OtherWork, TotalWorked, Paid, Comment, IsVacationWeek
        FROM WeekData
        WHERE strftime('%Y', WeekStart) = $year
        ORDER BY WeekStart";
        command.Parameters.AddWithValue("$year", year.ToString());

        using var reader = await command.ExecuteReaderAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"WorkJournal_{year}");

        // Headers
        string[] headers = { "WeekStart", "SaturdayHours", "SundayHours", "KilometersDriven",
                         "DaysWorked", "HoursDriven", "OtherWork", "TotalWorked",
                         "Paid", "Comment", "IsVacationWeek" };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        while (await reader.ReadAsync())
        {
            bool isVacation = reader.GetInt32(10) == 1;

            worksheet.Cell(row, 1).Value = reader.GetString(0); // WeekStart
            worksheet.Cell(row, 2).Value = reader.GetInt32(1);   // SaturdayHours
            worksheet.Cell(row, 3).Value = reader.GetInt32(2);   // SundayHours
            worksheet.Cell(row, 4).Value = reader.GetInt32(3);   // KilometersDriven
            worksheet.Cell(row, 5).Value = reader.GetInt32(4);   // DaysWorked
            worksheet.Cell(row, 6).Value = reader.GetInt32(5);   // HoursDriven
            worksheet.Cell(row, 7).Value = reader.IsDBNull(6) ? "" : reader.GetString(6); // OtherWork
            worksheet.Cell(row, 8).Value = reader.IsDBNull(7) ? "" : reader.GetString(7); // TotalWorked
            worksheet.Cell(row, 9).Value = reader.IsDBNull(8) ? "" : reader.GetString(8); // Paid
            worksheet.Cell(row, 10).Value = reader.IsDBNull(9) ? "" : reader.GetString(9); // Comment
            worksheet.Cell(row, 11).Value = isVacation ? "Yes" : "No"; // IsVacationWeek

            if (isVacation)
            {
                for (int col = 1; col <= headers.Length; col++)
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightCoral;
            }

            row++;
        }

        worksheet.Columns().AdjustToContents();

        // Build path in the same directory as the DB
        var folder = Path.GetDirectoryName(_dbPath)!;
        var filePath = Path.Combine(folder, $"WorkJournal_{year}.xlsx");

        workbook.SaveAs(filePath);

        return filePath; // return path to the saved Excel file
    }
}
