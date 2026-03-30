using System.Text.Json;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TimeManager.Tests")]

namespace TimeManager;

public enum Category { Debt, Growth, Other }

public enum Activity { Programming, Research, Documentation, Learning, Meeting, Administration, Other }

public class TimeEntry
{
    public Guid Id { get; set; } = Guid.Empty;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public Activity Activity { get; set; }
    public TimeSpan Duration { get; set; }
}

public static class TimeEntryStore
{
    // Make these internal and writable so tests can point the store to a temp folder.
    internal static string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TimeManager");

    internal static string DataFile = Path.Combine(DataFolder, "entries.json");

    public static void Save(TimeEntry entry)
    {
        Directory.CreateDirectory(DataFolder);

        if (entry.Id == Guid.Empty)
            entry.Id = Guid.NewGuid();

        var entries = Load();
        entries.Add(entry);

        WriteAll(entries);
    }

    public static void Update(TimeEntry updated)
    {
        var entries = Load();
        var idx = entries.FindIndex(e => e.Id == updated.Id);
        if (idx >= 0)
        {
            entries[idx] = updated;
            WriteAll(entries);
        }
    }

    public static List<TimeEntry> Load()
    {
        if (!File.Exists(DataFile))
            return new List<TimeEntry>();

        var json = File.ReadAllText(DataFile);
        return JsonSerializer.Deserialize<List<TimeEntry>>(json) ?? new List<TimeEntry>();
    }

    public static List<TimeEntry> GetWeek(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        return Load()
            .Where(e => e.Date.Date >= weekStart.Date && e.Date.Date < weekEnd.Date)
            .OrderBy(e => e.Date)
            .ToList();
    }

    private static void WriteAll(List<TimeEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DataFile, json);
    }
}
