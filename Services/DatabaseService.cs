using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Globalization;
using WorkJournal.Entities;

namespace WorkJournal.Services;

public class DatabaseService
{
    private ILogger<DatabaseService> logger;

    private readonly string _dbPath;
    public string? ExportFolderUri { get; private set; }

    public bool IsInitialized { get; private set; }

    public DatabaseService(ILogger<DatabaseService> logger)
    {
        this.logger = logger;

#if ANDROID
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "workcalendar.db");
#else
        _dbPath = Path.Combine(AppContext.BaseDirectory, "workcalendar.db");
#endif
    }

    // Call this after constructor to initialize DB asynchronously
    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

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
        await command.ExecuteNonQueryAsync();

        IsInitialized = true;
    }

    // Existing async methods remain the same
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

#if ANDROID
    public Task SetExportFolderAsync(string folderUri)
    {
        ExportFolderUri = folderUri;
        return Task.CompletedTask;
    }
#endif

    public async Task<string> ExportYearToExcelAsync(int year)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        // Load all week data for the year into memory
        var weekData = new Dictionary<DateTime, (bool IsVacation, object?[] Data)>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
            SELECT WeekStart, SaturdayHours, SundayHours, KilometersDriven, DaysWorked,
                   HoursDriven, OtherWork, TotalWorked, Paid, Comment, IsVacationWeek
            FROM WeekData
            WHERE strftime('%Y', WeekStart) = $year";
            command.Parameters.AddWithValue("$year", year.ToString());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var weekStart = DateTime.Parse(reader.GetString(0));
                bool isVacation = reader.GetInt32(10) == 1;

                var data = new object?[]
                {
                reader.GetInt32(1), // SaturdayHours
                reader.GetInt32(2), // SundayHours
                reader.GetInt32(3), // Km
                reader.GetInt32(4), // DaysWorked
                reader.GetInt32(5), // HoursDriven
                reader.IsDBNull(6) ? "" : reader.GetString(6),
                reader.IsDBNull(7) ? "" : reader.GetString(7),
                reader.IsDBNull(8) ? "" : reader.GetString(8),
                reader.IsDBNull(9) ? "" : reader.GetString(9)
                };

                weekData[weekStart] = (isVacation, data);
            }
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"Kalendorius {year}");

        // Headers
        string[] headers =
        {
        "Mėnuo", "Pn", "An", "Tr", "Kt", "Pe", "Št", "Sk",
        "Šeš.val.", "Sek.val.", "Pravažiuota (Km)", "Dirbta dienų",
        "Vairuota val.", "Kiti darbai", "Viso išdirbta", "Išmokėta", "Komentaras"
    };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 2;
        var culture = new CultureInfo("lt-LT");

        for (int month = 1; month <= 12; month++)
        {
            var firstDay = new DateTime(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);

            // Month name (vertical)
            int monthStartRow = row;
            ws.Cell(row, 1).Value = culture.DateTimeFormat.MonthNames[month - 1];
            ws.Cell(row, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(row, 1).Style.Font.Bold = true;

            // Generate calendar weeks
            var weeks = new List<List<DateTime?>>();
            var week = new List<DateTime?>();

            int leading = ((int)firstDay.DayOfWeek + 6) % 7;
            for (int i = 0; i < leading; i++) week.Add(null);

            for (int d = 1; d <= daysInMonth; d++)
            {
                week.Add(new DateTime(year, month, d));
                if (week.Count == 7)
                {
                    weeks.Add(week);
                    week = new List<DateTime?>();
                }
            }

            if (week.Count > 0)
            {
                while (week.Count < 7) week.Add(null);
                weeks.Add(week);
            }

            foreach (var w in weeks)
            {
                DateTime? weekStart = w.FirstOrDefault(d => d.HasValue);
                bool isVacation = weekStart.HasValue && weekData.ContainsKey(weekStart.Value) && weekData[weekStart.Value].IsVacation;

                // Days
                for (int d = 0; d < 7; d++)
                    ws.Cell(row, d + 2).Value = w[d]?.Day ?? (XLCellValue)"";

                // Totals
                if (weekStart.HasValue && weekData.TryGetValue(weekStart.Value, out var data))
                {
                    for (int i = 0; i < data.Data.Length; i++)
                    {
                        var value = data.Data[i];

                        ws.Cell(row, i + 9).Value = value switch
                        {
                            null => "",
                            int intValue => intValue,
                            double doubleValue => doubleValue,
                            string strValue => strValue,
                            bool boolValue => boolValue,
                            _ => value.ToString() // fallback
                        };
                    }
                }

                // Vacation coloring
                if (isVacation)
                {
                    for (int c = 1; c <= headers.Length; c++)
                        ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.LightCoral;
                }

                row++;
            }

            ws.Range(monthStartRow, 1, row - 1, 1).Merge();
        }

        ws.Columns().AdjustToContents();

#if ANDROID
        if (string.IsNullOrWhiteSpace(ExportFolderUri))
            throw new InvalidOperationException("Export folder not selected");

        var context = Android.App.Application.Context ??
                      throw new InvalidOperationException("Android context unavailable");

        // Parse the tree URI returned by FolderPicker
        var treeUriString = ExportFolderUri!.Replace("%3A", ":");
        var treeUri = Android.Net.Uri.Parse(treeUriString);

        logger.LogError("Will try to save to tree URI: {uri}", treeUri);

        if (treeUri == null)
            throw new InvalidOperationException("Invalid folder URI");

        if (context.ContentResolver == null)
            throw new InvalidOperationException("Content resolver is null");

        // Extract the tree document ID
        var treeDocId = Android.Provider.DocumentsContract.GetTreeDocumentId(treeUri);

        // Build a proper folder URI under the tree
        var folderUri = Android.Provider.DocumentsContract.BuildDocumentUriUsingTree(treeUri, treeDocId);

        if (folderUri == null)
            throw new InvalidOperationException("Folder URI is null");

        var fileName = $"WorkJournal_{year}.xlsx";
        var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Now create the document in that folder
        var docUri = Android.Provider.DocumentsContract.CreateDocument(
            context.ContentResolver,
            folderUri,
            mimeType,
            fileName
        );

        if (docUri == null)
            throw new InvalidOperationException("Failed to create Excel document");

        using var stream = context.ContentResolver.OpenOutputStream(docUri)
            ?? throw new InvalidOperationException("Failed to open output stream");

        // Save workbook to the stream
        workbook.SaveAs(stream);

        return fileName;
#else
        var folder = Path.GetDirectoryName(_dbPath)!;
        var filePath = Path.Combine(folder, $"WorkJournal_{year}.xlsx");

        workbook.SaveAs(filePath);
        return filePath;
#endif
    }
}
