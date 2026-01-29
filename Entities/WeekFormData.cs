namespace WorkJournal.Entities;

public class WeekFormData
{
    public int SaturdayHours { get; set; } = 0;
    public int SundayHours { get; set; } = 0;
    public int KilometersDriven { get; set; } = 0;
    public int DaysWorked { get; set; } = 0;
    public int HoursDriven { get; set; } = 0;
    public string OtherWork { get; set; } = "";
    public string TotalWorked { get; set; } = "";
    public string Paid { get; set; } = "";
    public string Comment { get; set; } = "";
}
