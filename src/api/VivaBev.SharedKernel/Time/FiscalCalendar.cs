namespace VivaBev.SharedKernel.Time;

public static class FiscalCalendar
{
    /// <summary>The fiscal year starts in July: FY2027 runs 1 July 2026 to 30 June 2027.</summary>
    public const int FirstMonth = 7;

    /// <summary>Fiscal years are named after the calendar year they end in.</summary>
    public static int FiscalYearOf(DateTime date) =>
        date.Month >= FirstMonth ? date.Year + 1 : date.Year;
}
