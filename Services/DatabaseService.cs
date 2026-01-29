using Microsoft.Data.Sqlite;
using WorkJournal.Entities;

namespace WorkJournal.Services;

public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "workcalendar.db");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS WeekData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WeekStart TEXT NOT NULL,
                SaturdayHours INTEGER,
                SundayHours INTEGER,
                KilometersDriven INTEGER,
                DaysWorked INTEGER,
                HoursDriven INTEGER,
                OtherWork TEXT,
                TotalWorked TEXT,
                Paid TEXT,
                Comment TEXT
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
            (WeekStart, SaturdayHours, SundayHours, KilometersDriven, DaysWorked, HoursDriven, OtherWork, TotalWorked, Paid, Comment)
            VALUES ($weekStart, $sat, $sun, $km, $days, $hours, $other, $total, $paid, $comment)";

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

        await command.ExecuteNonQueryAsync();
    }

    public async Task<WeekFormData?> LoadWeekDataAsync(DateTime weekStart)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT SaturdayHours, SundayHours, KilometersDriven, DaysWorked, HoursDriven,
                   OtherWork, TotalWorked, Paid, Comment
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
                Comment = reader.IsDBNull(8) ? "" : reader.GetString(8)
            };
        }

        return null;
    }
}
